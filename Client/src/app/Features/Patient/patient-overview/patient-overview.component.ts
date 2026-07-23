import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { PatientService } from '../services/patient.service';
import { BodyPainMapComponent, BodyPainMapPayload } from '../../intake/components/body-pain-map/body-pain-map.component';
import { DynamicFormRendererComponent } from '../../intake/components/dynamic-form-renderer/dynamic-form-renderer.component';
import { DynamicFormSubmissionDto } from '../../intake/models';
import { AgePipe } from '../../../Shared/Pipes/age-pipe';

@Component({
  selector: 'app-patient-overview',
  standalone: true,
  imports: [CommonModule, ButtonModule, BodyPainMapComponent, DynamicFormRendererComponent, AgePipe],
  templateUrl: './patient-overview.component.html',
  styleUrl: './patient-overview.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientOverviewComponent implements OnInit {
  private readonly patientService = inject(PatientService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  // Left as `any` to match what PatientService.getOverview()/getFormSchema()
  // already returned before this conversion — no shape change, just signals.
  patient = signal<any>(null);
  isLoading = signal(false);

  submissionData = signal<any>(null);
  formSchema = signal<any>(null);
  doctorInfo = signal<{ chiefComplaint?: string; patientCategory?: string } | null>(null);
  painMapPayload = signal<BodyPainMapPayload | null>(null);

  isEditMode = signal(false);
  isSaving = signal(false);
  flatInitialAnswers = signal<Record<string, any> | null>(null);
  private pendingSubmission: DynamicFormSubmissionDto | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

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

        let doctorInfo: { chiefComplaint?: string; patientCategory?: string } | null = null;
        if (data.doctorInfoJson) {
          try {
            doctorInfo = JSON.parse(data.doctorInfoJson);
          } catch {
            doctorInfo = null;
          }
        }
        this.doctorInfo.set(doctorInfo);

        if (regions.length > 0 || doctorInfo) {
          this.painMapPayload.set({
            regions,
            chiefComplaint: doctorInfo?.chiefComplaint ?? '',
            patientCategory: (doctorInfo?.patientCategory ?? '') as any,
          });
        }

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
    this.isEditMode.set(true);
  }

  cancelEdit(): void {
    this.isEditMode.set(false);
    this.pendingSubmission = null;
  }

  onSubmissionChange(submission: DynamicFormSubmissionDto): void {
    this.pendingSubmission = submission;
  }

  saveEdit(): void {
    const patientId = this.patient()?.id;
    if (!this.pendingSubmission || !patientId) return;

    this.isSaving.set(true);
    const body = { formSubmissionData: JSON.stringify(this.pendingSubmission) };

    this.patientService.updateOverviewSubmission(patientId, body).subscribe({
      next: () => {
        this.submissionData.set(this.pendingSubmission);
        this.isEditMode.set(false);
        this.isSaving.set(false);
      },
      error: err => {
        console.error(err);
        this.isSaving.set(false);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/app/patients', this.patient()?.id]);
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