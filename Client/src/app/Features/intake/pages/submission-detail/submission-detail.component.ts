import { Component, inject, OnInit, signal, computed, DestroyRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { ConfirmationService } from 'primeng/api';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { IntakeApiService } from '../../services/intake-api.service';
import { DynamicFormEngineService } from '../../services/dynamic-form-engine.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import { AuthService } from '../../../../Core/Services/auth.service';
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
  getIntakeStatusKey,
} from '../../models';

import { ConvertToPatientDialogComponent } from './convert-to-patient-dialog/convert-to-patient-dialog.component';
import { SubmissionSummaryCardComponent } from './submission-summary-card/submission-summary-card.component';
import { SubmittedAnswersViewerComponent } from './submitted-answers-viewer/submitted-answers-viewer.component';

interface DetailAction {
  type: 'status' | 'convert';
  status: IntakeStatus;
  labelKey: string;
  icon: string;
  severity: 'info' | 'warn' | 'success' | 'danger' | 'secondary' | 'contrast';
  messageKey: string;
}

@Component({
  selector: 'app-submission-detail',
  standalone: true,
  imports: [
    CommonModule,
    ConfirmDialogModule,
    DialogModule,
    TranslocoModule,
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
  private readonly auth = inject(AuthService);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly submissionId = signal<string | null>(null);

  private static readonly CONVERT_PERMISSION = 'Intake:Convert';

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

   @ViewChild(SubmittedAnswersViewerComponent) private answersViewer?: SubmittedAnswersViewerComponent;

  readonly chiefComplaintDisplay = computed(() => {
    const questionId = this.findQuestionIdByText('Chief Complaint') ?? 'question_default_chief_complaint';
    return this.extractAnswer(questionId);
  });
  readonly patientCategoryDisplay = computed(() => {
    const questionId = this.findQuestionIdByText('Patient Type') ?? 'question_default_patient_type';
    return this.extractAnswer(questionId);
  });

  readonly patientNameDisplay = computed(() => {
    const questionId = this.findQuestionIdByText('Full Name') ?? this.findQuestionIdByText('Name') ?? 'question_default_full_name';
    return this.extractAnswer(questionId);
  });
  readonly patientEmailDisplay = computed(() => {
    const questionId = this.findQuestionIdByText('Email') ?? this.findQuestionIdByText('E-mail') ?? 'question_default_email';
    return this.extractAnswer(questionId);
  });
  readonly patientPhoneDisplay = computed(() => {
    const questionId = this.findQuestionIdByText('Phone') ?? this.findQuestionIdByText('Phone Number') ?? 'question_default_phone';
    return this.extractAnswer(questionId);
  });

  // --- Permissions ---

  /** Only users holding Intake:Convert (doctors) may edit or convert. Receptionists
   *  never see Edit or the Convert action, regardless of submission status. */
  readonly canEditPermission = computed(() =>
    this.auth.hasPermission(SubmissionDetailComponent.CONVERT_PERMISSION)
  );

  readonly canEdit = computed(() => {
    const status = this.details()?.status;
    const statusOk = status != null && status !== IntakeStatus.Converted && status !== IntakeStatus.Expired;
    return statusOk && this.canEditPermission();
  });

  

  // --- Conversion validation (runs whether editing or not — quick-convert from
  //     Approved status must be validated too, not just the edit-mode path) ---

  readonly conversionValidation = computed<{ isValid: boolean; missing: string[] }>(() => {
    const chiefComplaint = this.chiefComplaintDisplay();
    const patientCategory = this.patientCategoryDisplay();
    const submission = this.isEditing() ? this.editedSubmission() : this.submissionData();

    const missing: string[] = [];
    if (!chiefComplaint?.trim()) missing.push('Chief Complaint');
    if (!patientCategory) missing.push('Patient Category');
    missing.push(...this.getMissingRequiredQuestions(submission));

    return { isValid: missing.length === 0, missing };
  });

  private getMissingRequiredQuestions(submission: DynamicFormSubmissionDto | null): string[] {
    const s = this.schema();
    if (!s || !submission) return [];

    const answered = new Map<string, any>();
    for (const section of submission.sections) {
      for (const group of section.groups) {
        for (const answer of group.answers) {
          answered.set(answer.questionId, this.unwrapAnswerValue(answer.value));
        }
      }
    }

    const missing: string[] = [];
    for (const question of this.engine.getAllQuestions(s)) {
      if (!question.required) continue;
      const value = answered.get(question.questionId);
      const isEmpty = value == null || value === '' || (Array.isArray(value) && value.length === 0);
      if (isEmpty) missing.push(question.text);
    }
    return missing;
  }

  private findQuestionIdByText(text: string): string | undefined {
    const s = this.schema();
    if (!s) return undefined;
    for (const section of s.sections) {
      for (const group of section.groups) {
        for (const question of group.questions) {
          if (question.text.toLowerCase() === text.toLowerCase()) {
            return question.questionId;
          }
        }
      }
    }
    return undefined;
  }

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

  readonly availableActions = computed<DetailAction[]>(() => {
    const current = this.details()?.status;
    if (current == null) return [];
    const canConvert = this.canEditPermission();

    switch (current) {
      case IntakeStatus.Pending:
      case IntakeStatus.Submitted:
        return [{
          type: 'status',
          status: IntakeStatus.InReview,
          labelKey: 'intake.action.markInReview',
          icon: 'pi pi-eye',
          severity: 'info' as const,
          messageKey: 'intake.action.msgMarkInReview'
        },
        {
          type: 'status',
          status: IntakeStatus.Rejected,
          labelKey: 'intake.action.reject',
          icon: 'pi pi-times-circle',
          severity: 'danger' as const,
          messageKey: 'intake.action.msgRejectShort'
        }];
      case IntakeStatus.InReview:
        return [{
          type: 'status',
          status: IntakeStatus.Rejected,
          labelKey: 'intake.action.reject',
          icon: 'pi pi-times-circle',
          severity: 'danger' as const,
          messageKey: 'intake.action.msgRejectShort'
        }];
      case IntakeStatus.Approved:
        return [
          ...(canConvert ? [{
            type: 'convert' as const,
            status: IntakeStatus.Converted,
            labelKey: 'intake.action.convertToPatient',
            icon: 'pi pi-user-plus',
            severity: 'success' as const,
            messageKey: ''
          }] : []),
          {
            type: 'status' as const,
            status: IntakeStatus.Rejected,
            labelKey: 'intake.action.reject',
            icon: 'pi pi-times-circle',
            severity: 'danger' as const,
            messageKey: 'intake.action.msgReject'
          }
        ];
      case IntakeStatus.Rejected:
        return [{
          type: 'status',
          status: IntakeStatus.InReview,
          labelKey: 'intake.action.reopenReview',
          icon: 'pi pi-undo',
          severity: 'info' as const,
          messageKey: 'intake.action.msgReopenReview'
        }];
      default:
        return [];
    }
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
      this.error.set(this.transloco.translate('intake.detail.invalidId'));
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
        this.error.set(this.transloco.translate('intake.detail.errorMessage'));
        this.loading.set(false);
      }
    });
  }

  // --- Edit mode ---

  startEditing(): void {
    if (!this.canEditPermission()) {
      this.snackbar.error(
        this.transloco.translate('intake.detail.errors.notPermitted'),
        [this.transloco.translate('intake.detail.errors.notPermittedBody')],
      );
      return;
    }
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

  attemptConvert(): void {
    if (!this.canEditPermission()) {
      this.snackbar.error(this.transloco.translate('intake.snackbar.notPermitted'), [
        this.transloco.translate('intake.snackbar.noPermissionConvert')
      ]);
      return;
    }

    const validation = this.conversionValidation();
    if (!validation.isValid) {
      if (this.isEditing()) {
        this.answersViewer?.markAllTouched();
      }
      this.snackbar.error(this.transloco.translate('intake.snackbar.missingRequiredFields'), [
        this.transloco.translate('intake.snackbar.pleaseComplete', { fields: validation.missing.join(', ') })
      ]);
      return;
    }

    this.showConvertDialog.set(true);
  }

  confirmUpdate(action: DetailAction): void {
    if (action.type === 'convert') {
      this.attemptConvert();
      return;
    }
    this.confirmationService.confirm({
      message: action.messageKey ? this.transloco.translate(action.messageKey) : '',
      header: this.transloco.translate('intake.confirm.confirmAction'),
      icon: 'pi pi-info-circle',
      acceptLabel: this.transloco.translate('intake.confirm.yesProceed'),
      rejectLabel: this.transloco.translate('intake.common.cancel'),
      acceptButtonStyleClass: 'p-button-primary',
      rejectButtonStyleClass: 'p-button-secondary',
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
        this.snackbar.success(this.transloco.translate('intake.snackbar.statusUpdated'), [
          this.transloco.translate('intake.snackbar.submissionMovedTo', { status: this.getStatusLabel(newStatus) })
        ]);
        this.updating.set(false);
        this.isEditing.set(false);
        this.editedSubmission.set(null);
        this.editedPainMap.set(null);
        this.loadDetails();
      },
      error: (err: any) => {
        this.updating.set(false);
        const msg = err?.error?.detail || err?.error?.title || this.transloco.translate('intake.snackbar.unexpectedError');
        this.snackbar.error(this.transloco.translate('intake.snackbar.updateFailed'), [msg]);
      }
    });
  }

  convertToPatient(): void {
    const id = this.submissionId();
    if (!id) return;

    if (!this.canEditPermission()) {
      this.snackbar.error(this.transloco.translate('intake.snackbar.notPermitted'), [
        this.transloco.translate('intake.snackbar.noPermissionConvert')
      ]);
      return;
    }

    // Validate BEFORE touching `updating` or making the request — the previous
    // version only checked this while isEditing() was true, so the quick-convert
    // path (straight from Approved status) skipped validation entirely and the
    // request silently failed against the backend with no visible feedback.
    const validation = this.conversionValidation();
    if (!validation.isValid) {
      this.snackbar.error(this.transloco.translate('intake.snackbar.missingRequiredFields'), [
        this.transloco.translate('intake.snackbar.pleaseComplete', { fields: validation.missing.join(', ') })
      ]);
      return;
    }

    this.updating.set(true);

    const painMap = this.isEditing() ? this.editedPainMap() : this.painMapPayload();
    const chiefComplaint = this.chiefComplaintDisplay();
    const submission = this.isEditing() ? this.editedSubmission() : this.submissionData();

    const request: ConvertIntakeToPatientRequest = {
      formSubmissionData: submission ? JSON.stringify(submission) : (this.details()?.formSubmissionData ?? '{}'),
      painPointsData: painMap && painMap.regions && painMap.regions.length > 0 ? JSON.stringify(painMap) : (this.details()?.painPointsData ?? undefined),
    };

    this.intakeApi.convertToPatient(id, request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res: PreVisitIntakeResponse) => {
        this.snackbar.success(this.transloco.translate('intake.snackbar.conversionSuccessful'), [
          this.transloco.translate('intake.snackbar.convertedToPatientRecord')
        ]);
        this.updating.set(false);
        this.showConvertDialog.set(false);
        this.isEditing.set(false);
        this.editedSubmission.set(null);
        this.editedPainMap.set(null);

        if (res?.convertedToPatientId) {
          this.router.navigate(['/app/initial-report', res.convertedToPatientId], {
            state: {
              patient: {
                id: res.convertedToPatientId,
                name: res.patientName ?? this.patientNameDisplay(),
                chiefComplaint: chiefComplaint,
              }
            }
          });
        } else {
          this.router.navigate(['/app/intake/submissions']);
        }
      },
      error: (err: any) => {
        this.updating.set(false);
        const msg = err?.error?.detail || err?.error?.title || err?.error?.message || this.transloco.translate('intake.snackbar.unexpectedError');
        this.snackbar.error(this.transloco.translate('intake.snackbar.conversionFailed'), [msg]);
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
    return this.formatValueRecursive(answer?.value);
  }

  formatValueRecursive(val: any): string {
    if (val == null || val === '') return '—';

    if (typeof val === 'boolean') return val ? 'Yes' : 'No';

    if (typeof val === 'string' || typeof val === 'number') {
      const str = String(val).trim();
      return str || '—';
    }

    if (Array.isArray(val)) {
      if (val.length === 0) return '—';
      const items = val.map(item => this.formatValueRecursive(item)).filter(item => item !== '—');
      return items.length > 0 ? items.join(', ') : '—';
    }

    if (typeof val === 'object') {
      const dict = val as Record<string, any>;
      const keys = Object.keys(dict);
      if (keys.length === 0) return '—';

      if (keys.length === 1) {
        return this.formatValueRecursive(dict[keys[0]]);
      }

      const pairs: string[] = [];
      for (const key of keys) {
        const itemVal = dict[key];
        if (itemVal != null && itemVal !== '') {
          const formatted = this.formatValueRecursive(itemVal);
          if (formatted !== '—') {
            const cleanKey = key
              .replace(/([A-Z])/g, ' $1')
              .replace(/^./, str => str.toUpperCase())
              .trim();
            pairs.push(`${cleanKey}: ${formatted}`);
          }
        }
      }
      return pairs.length > 0 ? pairs.join(' · ') : '—';
    }

    return String(val);
  }

  getStatusLabel(status: IntakeStatus): string {
    return this.transloco.translate(getIntakeStatusKey(status));
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