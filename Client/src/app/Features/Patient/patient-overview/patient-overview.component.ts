import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { PatientService } from '../services/patient.service';
import { BodyPainMapComponent, BodyPainMapPayload } from '../../intake/components/body-pain-map/body-pain-map.component';
import { DynamicFormRendererComponent } from '../../intake/components/dynamic-form-renderer/dynamic-form-renderer.component';
import { DynamicFormSubmissionDto } from '../../intake/models';
import { AgePipe } from '../../../Shared/Pipes/age-pipe';
import { PatientScheduleOverviewDto } from '../../../Shared/Models/Patient.model';
import { PatientScheduleOverviewComponent } from '../patient-schedule-overview/patient-schedule-overview.component';

@Component({
  selector: 'app-patient-overview',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    BodyPainMapComponent,
    DynamicFormRendererComponent,
    AgePipe,
    PatientScheduleOverviewComponent,
  ],
  templateUrl: './patient-overview.component.html',
  styleUrl: './patient-overview.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientOverviewComponent implements OnInit {
  private readonly patientService = inject(PatientService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  scheduleOverview = signal<PatientScheduleOverviewDto | null>(null);

  patient = signal<any>(null);
  isLoading = signal(false);

  submissionData = signal<any>(null);
  formSchema = signal<any>(null);

  painMapPayload = signal<BodyPainMapPayload | null>(null);

  isEditMode = signal(false);
  isSaving = signal(false);
  flatInitialAnswers = signal<Record<string, any> | null>(null);
  pendingPainMap = signal<({ regions: any[] } & Record<string, any>) | null>(null);

  private pendingSubmission: DynamicFormSubmissionDto | null = null;

  // Groups are split purely on the schema's `hiddenFromPatient` flag (same flag the
  // DynamicFormRendererComponent itself uses to decide what the public intake form shows).
  // Patient-visible groups render in "Patient Details"; hiddenFromPatient groups render in
  // "Clinical Summary" (doctor-only). Chief Complaint / Patient Category now live as regular
  // questions inside these groups rather than in a separate doctorInfoJson blob.
  readonly patientVisibleSections = computed(() => this.filterSectionsByVisibility(false));
  readonly doctorOnlySections = computed(() => this.filterSectionsByVisibility(true));
  readonly hasDoctorOnlyContent = computed(() => this.doctorOnlySections().length > 0);

  private filterSectionsByVisibility(hiddenFromPatient: boolean): any[] {
    const schema = this.formSchema();
    if (!schema?.sections) return [];
    return schema.sections
      .map((section: any) => ({
        ...section,
        groups: (section.groups ?? []).filter((g: any) => !!g.hiddenFromPatient === hiddenFromPatient),
      }))
      .filter((section: any) => section.groups.length > 0);
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.loadOverview(id);

    this.patientService.getScheduleOverview(id).subscribe(overview => {
      this.scheduleOverview.set(overview);
    });
  }

  private loadOverview(id: string): void {
    this.isLoading.set(true);
    this.patientService.getOverview(id).subscribe({
      next: data => {
        this.patient.set(data);

        if (data.formSubmissionData) {
          try {
            this.submissionData.set(JSON.parse(data.formSubmissionData));
          } catch {
            this.submissionData.set(null);
          }
        }

        let regions: any[] = [];
        if (data.painPointsJson) {
          try {
            regions = JSON.parse(data.painPointsJson)?.regions ?? [];
          } catch {
            regions = [];
          }
        }
        this.painMapPayload.set(regions.length > 0 ? { regions } : null);

        const schemaId = this.submissionData()?.formSchemaId;

        if (schemaId) {
          this.patientService.getFormSchema(schemaId).subscribe({
            next: schemaResponse => {
              try {
                this.formSchema.set(JSON.parse(schemaResponse.schemaJson));
              } catch {
                this.formSchema.set(null);
              }
              this.isLoading.set(false);
            },
            error: err => {
              console.error('Failed to load form schema', err);
              this.isLoading.set(false);
            },
          });
        } else {
          this.isLoading.set(false);
        }
      },
      error: err => {
        console.error(err);
        this.isLoading.set(false);
      },
    });
  }

  // Look up a question's answer by questionId inside the raw submission tree (read-only display)
  getAnswerValue(questionId: string): any {
    const submission = this.submissionData();
    if (!submission?.sections) return null;
    for (const section of submission.sections) {
      for (const group of section.groups) {
        const answer = group.answers.find((a: any) => a.questionId === questionId);
        if (answer) {
          const val = answer.value;
          if (val == null || Object.keys(val).length === 0) return null;
          const key = Object.keys(val)[0];
          const raw = val[key];
          return Array.isArray(raw) ? raw.join(', ') : raw;
        }
      }
    }
    return null;
  }

  // Build a flat { questionId: value } object from the current submissionData,
  // for seeding DynamicFormRendererComponent's initialAnswers input
  private buildFlatAnswers(): Record<string, any> {
    const flat: Record<string, any> = {};
    const submission = this.submissionData();
    if (!submission?.sections) return flat;

    for (const section of submission.sections) {
      for (const group of section.groups) {
        for (const answer of group.answers) {
          const val = answer.value;
          if (val == null || Object.keys(val).length === 0) continue;
          const key = Object.keys(val)[0];
          flat[answer.questionId] = val[key];
        }
      }
    }
    return flat;
  }

  enterEditMode(): void {
    this.flatInitialAnswers.set(this.buildFlatAnswers());
    this.pendingSubmission = null;

    const currentPainMap = this.painMapPayload();
    this.pendingPainMap.set({ regions: currentPainMap?.regions ?? [] });

    this.isEditMode.set(true);
  }

  cancelEdit(): void {
    this.isEditMode.set(false);
    this.pendingSubmission = null;
    this.pendingPainMap.set(null);
  }

  onSubmissionChange(submission: DynamicFormSubmissionDto): void {
    this.pendingSubmission = submission;
  }

  onPainMapChange(payload: BodyPainMapPayload): void {
    // Pain map now only carries regions — Chief Complaint / Patient Category live as
    // regular schema questions and are edited via the dynamic form renderer instead.
    const current = this.pendingPainMap();
    this.pendingPainMap.set({
      ...(current ?? {}),
      regions: payload.regions,
    });
  }

  saveEdit(): void {
    const patientId = this.patient()?.id;
    if (!patientId) return;

    this.isSaving.set(true);

    const submissionToSave = this.pendingSubmission
      ? JSON.stringify(this.pendingSubmission)
      : this.patient()?.formSubmissionData;

    const regions = this.pendingPainMap()?.regions ?? [];
    const painMapToSave = JSON.stringify({ regions });

    this.patientService.updateOverviewSubmission(patientId, {
      formSubmissionData: submissionToSave,
      painPointsData: painMapToSave,
    }).subscribe({
      next: () => {
        this.isEditMode.set(false);
        this.isSaving.set(false);
        this.loadOverview(patientId);
      },
      error: err => {
        console.error(err);
        this.isSaving.set(false);
      },
    });
  }

  goToEdit(): void {
    const patientId = this.patient()?.id;
    if (!patientId) return;
    this.router.navigate(['/app/patients/edit', patientId]);
  }

  goToInitialReport(): void {
    const patientId = this.patient()?.id;
    if (!patientId) return;
    this.router.navigate(['/app/initial-report', patientId]);
  }

  delete(): void {
    const patientId = this.patient()?.id;
    if (!patientId) return;
    if (confirm('Are you sure you want to delete this patient?')) {
      this.patientService.delete(patientId).subscribe({
        next: () => this.router.navigate(['/app/patients']),
        error: err => console.error(err),
      });
    }
  }

  goBack(): void {
    this.router.navigate(['/app/patients']);
  }

  // NOTE: assumed route — this is the receptionist-scheduling route you built
  // earlier (`receptionist-scheduling/:patientId`). Adjust the path segment
  // below if it's registered under a different parent path.
  continueScheduling(): void {
    const patientId = this.patient()?.id;
    if (!patientId) return;
    this.router.navigate(['/app/receptionist-scheduling', patientId]);
  }
}