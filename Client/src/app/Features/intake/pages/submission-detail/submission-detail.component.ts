import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { ConfirmationService } from 'primeng/api';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IntakeApiService } from '../../services/intake-api.service';
import { DynamicFormEngineService } from '../../services/dynamic-form-engine.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import { DynamicFormRendererComponent } from '../../components/dynamic-form-renderer/dynamic-form-renderer.component';
import { BodyPainMapComponent, BodyPainMapPayload } from '../../components/body-pain-map/body-pain-map.component';
import {
  PreVisitIntakeDetailsResponse,
  PreVisitIntakeResponse,
  DynamicFormSchemaDto,
  DynamicFormSubmissionDto,
  IntakeStatus,
  SubmissionAnswerDto,
  FormQuestionDto,
  UpdateIntakeStatusRequest,
  ConvertIntakeToPatientRequest,
} from '../../models';

import { ConvertToPatientDialogComponent } from './convert-to-patient-dialog/convert-to-patient-dialog.component';
import { SubmissionSummaryCardComponent } from './submission-summary-card/submission-summary-card.component';
import { SubmittedAnswersViewerComponent } from './submitted-answers-viewer/submitted-answers-viewer.component';

@Component({
  selector: 'app-submission-detail',
  standalone: true,
  imports: [
    CommonModule,
    ConfirmDialogModule,
    DialogModule,
    BodyPainMapComponent,
    ConvertToPatientDialogComponent,
    SubmissionSummaryCardComponent,
    SubmittedAnswersViewerComponent,
  ],
  providers: [ConfirmationService],
  templateUrl: './submission-detail.component.html',
  styleUrl: './submission-detail.component.css'
})
export class SubmissionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly intakeApi = inject(IntakeApiService);
  private readonly engine = inject(DynamicFormEngineService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly submissionId = signal<string | null>(null);

  readonly loading = signal(true);
  readonly updating = signal(false);
  readonly error = signal<string | null>(null);
  readonly showConvertDialog = signal(false);
  readonly details = signal<PreVisitIntakeDetailsResponse | null>(null);
  readonly schema = signal<DynamicFormSchemaDto | null>(null);
  readonly submissionData = signal<DynamicFormSubmissionDto | null>(null);
  readonly painMapPayload = signal<BodyPainMapPayload | null>(null);

  // --- Edit mode state ---
  readonly isEditing = signal(false);
  readonly editIsValid = signal(true);
  readonly editedSubmission = signal<DynamicFormSubmissionDto | null>(null);
  readonly editedPainMap = signal<BodyPainMapPayload | null>(null);

  readonly hasPainData = computed(() => (this.painMapPayload()?.regions.length ?? 0) > 0);

  /** These three are extracted from submissionData() (or editedSubmission() while
   *  editing) rather than the backend response — PreVisitIntakeDetailsResponse no
   *  longer carries patientName/Email/Phone directly, but formSubmissionData is
   *  already included in this response, so no extra request is needed. Matches the
   *  same default question IDs the backend uses elsewhere (ConvertToPatientAsync,
   *  GetSubmissionsAsync) — same fragility caveat applies: breaks only if a doctor
   *  deletes and recreates these exact seeded questions. */
  readonly patientNameDisplay = computed(() => this.extractAnswer('question_default_full_name'));
  readonly patientEmailDisplay = computed(() => this.extractAnswer('question_default_email'));
  readonly patientPhoneDisplay = computed(() => this.extractAnswer('question_default_phone'));

  private extractAnswer(questionId: string): string | undefined {
    const data = this.isEditing() ? this.editedSubmission() : this.submissionData();
    if (!data) return undefined;
    for (const section of data.sections) {
      for (const group of section.groups) {
        for (const answer of group.answers) {
          if (answer.questionId === questionId) {
            const value = this.unwrapAnswerValue(answer.value);
            return value != null && value !== '' ? String(value) : undefined;
          }
        }
      }
    }
    return undefined;
  }

  /** Unwraps the stored submission's {questionId: {type: value}} shape into a flat
   *  {questionId: value} record so DynamicFormRendererComponent can be pre-filled. */
  readonly initialAnswersForEdit = computed<Record<string, any>>(() => {
    const data = this.submissionData();
    if (!data) return {};
    const result: Record<string, any> = {};
    for (const section of data.sections) {
      for (const group of section.groups) {
        for (const answer of group.answers) {
          result[answer.questionId] = this.unwrapAnswerValue(answer.value);
        }
      }
    }
    return result;
  });

  private readonly questionMap = computed<Record<string, FormQuestionDto>>(() => {
    const s = this.schema();
    if (!s) return {};
    const map: Record<string, FormQuestionDto> = {};
    for (const q of this.engine.getAllQuestions(s)) {
      map[q.questionId] = q;
    }
    return map;
  });

  private readonly sectionMap = computed<Record<string, string>>(() => {
    const s = this.schema();
    if (!s) return {};
    const map: Record<string, string> = {};
    for (const section of s.sections) {
      map[section.sectionId] = section.title;
    }
    return map;
  });

  private readonly groupMap = computed<Record<string, string>>(() => {
    const s = this.schema();
    if (!s) return {};
    const map: Record<string, string> = {};
    for (const section of s.sections) {
      for (const group of section.groups) {
        map[group.groupId] = group.title;
      }
    }
    return map;
  });

  readonly availableActions = computed<{ type: 'status' | 'convert'; status: IntakeStatus; label: string; icon: string; severity: 'info' | 'warn' | 'success' | 'danger' | 'secondary' | 'contrast'; message: string }[]>(() => {
    const current = this.details()?.status;
    if (current == null) return [];
    switch (current) {
      case IntakeStatus.Pending:
      case IntakeStatus.Submitted:
        return [{
          type: 'status',
          status: IntakeStatus.InReview,
          label: 'Mark In Review',
          icon: 'pi pi-eye',
          severity: 'info' as const,
          message: 'Mark this submission as in review?'
        },
        {
          type: 'status',
          status: IntakeStatus.Rejected,
          label: 'Reject',
          icon: 'pi pi-times-circle',
          severity: 'danger' as const,
          message: 'Reject this submission?'
        }];
      case IntakeStatus.InReview:
        return [{
          type: 'status',
          status: IntakeStatus.Rejected,
          label: 'Reject',
          icon: 'pi pi-times-circle',
          severity: 'danger' as const,
          message: 'Reject this submission?'
        }];
      case IntakeStatus.Approved:
        return [{
          type: 'convert',
          status: IntakeStatus.Converted,
          label: 'Convert to Patient',
          icon: 'pi pi-user-plus',
          severity: 'success' as const,
          message: ''
        },
        {
          type: 'status',
          status: IntakeStatus.Rejected,
          label: 'Reject',
          icon: 'pi pi-times-circle',
          severity: 'danger' as const,
          message: 'Reject this submission? This will mark the intake as rejected.'
        }];
      case IntakeStatus.Rejected:
        return [{
          type: 'status',
          status: IntakeStatus.InReview,
          label: 'Re-open Review',
          icon: 'pi pi-undo',
          severity: 'info' as const,
          message: 'Re-open this submission for review?'
        }];
      default:
        return [];
    }
  });

  /** Edit is available for anything that hasn't already been converted or expired —
   *  editing now leads straight into Convert to Patient, so it no longer depends on
   *  an Approve transition being on the table. */
  readonly canEdit = computed(() => {
    const status = this.details()?.status;
    return status != null && status !== IntakeStatus.Converted && status !== IntakeStatus.Expired;
  });

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      this.submissionId.set(params.get('id'));
      this.loadDetails();
    });
  }

  loadDetails(): void {
    const id = this.submissionId();
    if (!id) {
      this.error.set('Invalid submission ID.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.isEditing.set(false);

    this.intakeApi.getSubmissionDetails(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.details.set(data);

        try {
          const parsed = JSON.parse(data.formSubmissionData) as DynamicFormSubmissionDto;
          this.submissionData.set(parsed);
        } catch {
          // Submission data parsing failed
        }

        if (data.painPointsData) {
          try {
            this.painMapPayload.set(JSON.parse(data.painPointsData) as BodyPainMapPayload);
          } catch {
            // Pain map parsing failed
          }
        } else {
          this.painMapPayload.set(null);
        }

        this.intakeApi.getFormSchemaById(data.formSchemaId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: (schemaResponse) => {
            try {
              this.schema.set(this.engine.deserializeSchema(schemaResponse.schemaJson));
            } catch {
              // Schema parsing failed
            }
            this.loading.set(false);
          },
          error: () => {
            this.loading.set(false);
          }
        });
      },
      error: () => {
        this.error.set('Failed to load submission details. Please try again.');
        this.loading.set(false);
      }
    });
  }

  // --- Edit mode ---

  startEditing(): void {
    this.editedSubmission.set(this.submissionData());
    this.editedPainMap.set(this.painMapPayload());
    this.editIsValid.set(true);
    this.isEditing.set(true);
  }

  cancelEditing(): void {
    this.isEditing.set(false);
    this.editedSubmission.set(null);
    this.editedPainMap.set(null);
  }

  private unwrapAnswerValue(value: any): any {
    if (value != null && typeof value === 'object' && !Array.isArray(value)) {
      const keys = Object.keys(value);
      if (keys.length === 1) return value[keys[0]];
    }
    return value;
  }

  confirmUpdate(action: { type: 'status' | 'convert'; status: IntakeStatus; label: string; icon: string; severity: string; message: string }): void {
    if (action.type === 'convert') {
      this.showConvertDialog.set(true);
      return;
    }
    this.confirmationService.confirm({
      message: action.message,
      header: 'Confirm Action',
      icon: 'pi pi-info-circle',
      acceptLabel: 'Yes, Proceed',
      rejectLabel: 'Cancel',
      accept: () => this.updateStatus(action.status),
    });
  }

  updateStatus(newStatus: IntakeStatus): void {
    const id = this.submissionId();
    if (!id) return;

    this.updating.set(true);

    const request: UpdateIntakeStatusRequest = { newStatus };

    this.intakeApi.updateIntakeStatus(id, request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.snackbar.success('Status Updated', [`Submission moved to ${this.getStatusLabel(newStatus)}.`]);
        this.updating.set(false);
        this.isEditing.set(false);
        this.editedSubmission.set(null);
        this.editedPainMap.set(null);
        this.loadDetails();
      },
      error: (err: any) => {
        this.updating.set(false);
        const msg = err?.error?.detail || err?.error?.title || 'Could not update submission status.';
        this.snackbar.error('Update Failed', [msg]);
      }
    });
  }

  convertToPatient(): void {
    const id = this.submissionId();
    if (!id) return;

    this.updating.set(true);

    // Always send the current data — edited version if the doctor was editing,
    // otherwise whatever's already loaded. No conditional branching needed since
    // both fields are sent in every case now.
    const submission = this.isEditing() ? this.editedSubmission() : this.submissionData();
    const painMap = this.isEditing() ? this.editedPainMap() : this.painMapPayload();

    const request: ConvertIntakeToPatientRequest = {
      formSubmissionData: submission ? JSON.stringify(submission) : undefined,
      painPointsData: painMap && painMap.regions.length > 0 ? JSON.stringify(painMap) : undefined,
    };

    this.intakeApi.convertToPatient(id, request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res: PreVisitIntakeResponse) => {
        this.snackbar.success('Conversion Successful', ['Submission has been converted to a patient record.']);
        this.updating.set(false);
        this.showConvertDialog.set(false);
        this.isEditing.set(false);
        this.editedSubmission.set(null);
        this.editedPainMap.set(null);

        // works from a refresh / shared link.
        if (res?.convertedToPatientId) {
          this.router.navigate(['/app/initial-report', res.convertedToPatientId], {
            state: {
              patient: {
                id: res.convertedToPatientId,
                name: res.patientName ?? this.patientNameDisplay(),
                chiefComplaint: painMap?.chiefComplaint,
              }
            }
          });
        }else {
          this.router.navigate(['/app/intake/submissions']);
        }
      },
      error: (err: any) => {
        this.updating.set(false);
        const msg = err?.error?.detail || err?.error?.title || 'Could not convert submission to patient.';
        this.snackbar.error('Conversion Failed', [msg]);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/app/intake/submissions']);
  }

  getInitials(name: string | undefined): string {
    if (!name) return '?';
    return name.trim().split(/\s+/).map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getSectionTitle(sectionId: string): string | undefined {
    return this.sectionMap()[sectionId];
  }

  getGroupTitle(groupId: string): string | undefined {
    return this.groupMap()[groupId];
  }

  getQuestionText(questionId: string): string | undefined {
    return this.questionMap()[questionId]?.text;
  }

  formatAnswerValue(answer: SubmissionAnswerDto): string {
    if (answer.value == null) return '—';

    if (typeof answer.value === 'object' && !Array.isArray(answer.value)) {
      const dict = answer.value as Record<string, any>;
      const keys = Object.keys(dict);
      if (keys.length === 1) {
        const inner = dict[keys[0]];
        if (inner == null) return '—';
        if (Array.isArray(inner)) return inner.length === 0 ? '—' : inner.join(', ');
        if (typeof inner === 'boolean') return inner ? 'Yes' : 'No';
        return String(inner);
      }
      return String(answer.value);
    }

    if (Array.isArray(answer.value)) {
      if (answer.value.length === 0) return '—';
      return answer.value.join(', ');
    }
    if (typeof answer.value === 'boolean') return answer.value ? 'Yes' : 'No';
    return String(answer.value);
  }

  getStatusLabel(status: IntakeStatus): string {
    switch (status) {
      case IntakeStatus.Pending: return 'Pending';
      case IntakeStatus.Submitted: return 'Submitted';
      case IntakeStatus.InReview: return 'In Review';
      case IntakeStatus.Approved: return 'Approved';
      case IntakeStatus.Rejected: return 'Rejected';
      case IntakeStatus.Converted: return 'Converted';
      case IntakeStatus.Expired: return 'Expired';
      default: return 'Unknown';
    }
  }

  getStatusPillClass(status: IntakeStatus): string {
    switch (status) {
      case IntakeStatus.Pending:
      case IntakeStatus.Submitted:
      case IntakeStatus.InReview:
        return 'status-badge-warning';
      case IntakeStatus.Approved:
      case IntakeStatus.Converted:
        return 'status-badge-success';
      case IntakeStatus.Rejected:
      case IntakeStatus.Expired:
        return 'status-badge-danger';
      default:
        return 'status-badge-neutral';
    }
  }

  getActionButtonClass(severity: string): string {
    switch (severity) {
      case 'success':
      case 'info':
      case 'warn':
        return 'btn-action-primary';
      case 'danger':
        return 'btn-action-danger';
      default:
        return 'btn-action-secondary';
    }
  }
}