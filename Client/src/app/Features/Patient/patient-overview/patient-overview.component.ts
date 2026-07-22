import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PatientService } from '../services/patient.service';
import { BodyPainMapComponent, BodyPainMapPayload } from '../../intake/components/body-pain-map/body-pain-map.component';
import { DynamicFormRendererComponent } from '../../intake/components/dynamic-form-renderer/dynamic-form-renderer.component';
import { DynamicFormSubmissionDto } from '../../intake/models';
import { AgePipe } from '../../../Shared/Pipes/age-pipe';

@Component({
  selector: 'app-patient-overview',
  standalone: true,
  imports: [CommonModule, BodyPainMapComponent, DynamicFormRendererComponent, AgePipe],
  templateUrl: './patient-overview.component.html',
  styleUrl: './patient-overview.component.css',
})
export class PatientOverviewComponent implements OnInit {
  patient: any = null;
  isLoading = false;

  submissionData: any = null;
  formSchema: any = null;
  doctorInfo: { chiefComplaint?: string; patientCategory?: string } | null = null;
  painMapPayload: BodyPainMapPayload | null = null;

  isEditMode = false;
  isSaving = false;
  flatInitialAnswers: Record<string, any> | null = null;
  pendingSubmission: DynamicFormSubmissionDto | null = null;

  constructor(
    private patientService: PatientService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.isLoading = true;
    this.patientService.getOverview(id).subscribe({
      next: (data) => {
        this.patient = data;

        if (data.formSubmissionData) {
          try {
            this.submissionData = JSON.parse(data.formSubmissionData);
          } catch {
            this.submissionData = null;
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

        if (data.doctorInfoJson) {
          try {
            this.doctorInfo = JSON.parse(data.doctorInfoJson);
          } catch {
            this.doctorInfo = null;
          }
        }

        if (regions.length > 0 || this.doctorInfo) {
          this.painMapPayload = {
            regions,
            chiefComplaint: this.doctorInfo?.chiefComplaint ?? '',
            patientCategory: (this.doctorInfo?.patientCategory ?? '') as any
          };
        }

        const schemaId = this.submissionData?.formSchemaId;

        if (schemaId) {
          this.patientService.getFormSchema(schemaId).subscribe({
            next: (schemaResponse) => {
              try {
                this.formSchema = JSON.parse(schemaResponse.schemaJson);
              } catch {
                this.formSchema = null;
              }
              this.isLoading = false;
              this.cdr.detectChanges();
            },
            error: (err) => {
              console.error('Failed to load form schema', err);
              this.isLoading = false;
              this.cdr.detectChanges();
            }
          });
        } else {
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  // Look up a question's answer by questionId inside the raw submission tree (read-only display)
  getAnswerValue(questionId: string): any {
    if (!this.submissionData?.sections) return null;
    for (const section of this.submissionData.sections) {
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
    if (!this.submissionData?.sections) return flat;

    for (const section of this.submissionData.sections) {
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

  enterEditMode() {
    this.flatInitialAnswers = this.buildFlatAnswers();
    this.pendingSubmission = null;
    this.isEditMode = true;
  }

  cancelEdit() {
    this.isEditMode = false;
    this.pendingSubmission = null;
  }

  onSubmissionChange(submission: DynamicFormSubmissionDto) {
    this.pendingSubmission = submission;
  }

  saveEdit() {
    if (!this.pendingSubmission || !this.patient?.id) return;

    this.isSaving = true;
    const body = { formSubmissionData: JSON.stringify(this.pendingSubmission) };

    this.patientService.updateOverviewSubmission(this.patient.id, body).subscribe({
      next: () => {
        this.submissionData = this.pendingSubmission;
        this.isEditMode = false;
        this.isSaving = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.isSaving = false;
        this.cdr.detectChanges();
      }
    });
  }

  goBack() {
    this.router.navigate(['/app/patients', this.patient?.id]);
  }
}