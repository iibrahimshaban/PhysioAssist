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

@Component({
  selector: 'app-submission-detail',
  standalone: true,
  imports: [
    CommonModule,
    ConfirmDialogModule,
    DialogModule,
    DynamicFormRendererComponent,
    BodyPainMapComponent,
  ],
  providers: [ConfirmationService],
  template: `
    <p-confirmDialog
      [style]="{ width: '420px' }"
      ariaLabel="Confirmation dialog" />

    <!-- Convert Dialog -->
    <p-dialog
      header="Convert to Patient"
      [(visible)]="showConvertDialog"
      [modal]="true"
      [style]="{ width: '480px' }"
      [draggable]="false"
      [closable]="true"
      aria-labelledby="convert-dialog-header">

      <ng-template pTemplate="header">
        <div class="flex items-center gap-3">
          <div class="w-9 h-9 rounded-xl bg-emerald-500 flex items-center justify-center">
            <i class="pi pi-user-plus text-white text-sm"></i>
          </div>
          <div>
            <h3 class="font-bold text-base m-0 text-slate-900">Convert to Patient</h3>
            <p class="text-xs text-slate-500 m-0">Create a patient record from this intake</p>
          </div>
        </div>
      </ng-template>

      <div class="space-y-4">
        <p class="text-sm text-slate-600">
          This will create a new patient record from this intake submission. Review the details below and confirm.
        </p>
        <div class="rounded-xl border border-slate-200 overflow-hidden">
          <div class="bg-slate-50 px-4 py-2 border-b border-slate-100">
            <p class="text-xs font-bold text-slate-500 uppercase tracking-wider m-0">Patient Details</p>
          </div>
          <div class="p-4 space-y-3">
            <div>
              <p class="text-xs text-slate-400 uppercase tracking-wide m-0">Name</p>
              <p class="text-sm font-semibold text-slate-800 m-0 capitalize">{{ patientNameDisplay() || '—' }}</p>
            </div>
            @if (patientEmailDisplay()) {
              <div>
                <p class="text-xs text-slate-400 uppercase tracking-wide m-0">Email</p>
                <p class="text-sm text-slate-700 m-0">{{ patientEmailDisplay() }}</p>
              </div>
            }
            @if (patientPhoneDisplay()) {
              <div>
                <p class="text-xs text-slate-400 uppercase tracking-wide m-0">Phone</p>
                <p class="text-sm text-slate-700 m-0">{{ patientPhoneDisplay() }}</p>
              </div>
            }
          </div>
        </div>
      </div>
      <ng-template pTemplate="footer">
        <div class="flex gap-2 justify-end">
          <button
            type="button"
            (click)="showConvertDialog.set(false)"
            class="text-sm font-semibold px-5 py-2.5 rounded-full border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition"
            aria-label="Cancel conversion">
            Cancel
          </button>
          <button
            type="button"
            (click)="convertToPatient()"
            [disabled]="updating()"
            class="text-sm font-semibold px-5 py-2.5 rounded-full bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-60 shadow-sm transition flex items-center gap-1.5"
            aria-label="Confirm convert to patient">
            <i class="pi" [ngClass]="updating() ? 'pi-spin pi-spinner' : 'pi-user-plus'"></i>
            Confirm & create patient
          </button>
        </div>
      </ng-template>
    </p-dialog>

    <div class="max-w-4xl mx-auto py-8 px-4" aria-live="polite">

      <!-- Loading State -->
      @if (loading()) {
        <div class="flex flex-col items-center justify-center py-24 animate-fade-in" role="status">
          <div class="premium-spinner mb-4"></div>
          <p class="text-slate-500 text-sm">Loading submission details...</p>
        </div>
      }

      <!-- Error State -->
      @else if (error()) {
        <div class="max-w-lg mx-auto mt-8 animate-fade-in-up">
          <div class="bg-white rounded-2xl shadow-sm border border-rose-100 p-8 text-center" role="alert">
            <div class="w-16 h-16 rounded-full bg-rose-50 flex items-center justify-center mx-auto mb-4">
              <i class="pi pi-exclamation-triangle text-2xl text-rose-400"></i>
            </div>
            <h2 class="text-lg font-bold text-slate-800 mb-2">Failed to Load</h2>
            <p class="text-slate-600 text-sm mb-6">{{ error() }}</p>
            <div class="flex gap-3 justify-center">
              <button type="button" (click)="goBack()" class="text-sm font-semibold px-4 py-2 rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-50 transition flex items-center gap-1.5">
                <i class="pi pi-arrow-left"></i> Go Back
              </button>
              <button type="button" (click)="loadDetails()" class="text-sm font-semibold px-4 py-2 rounded-xl bg-amber-500 text-white hover:bg-amber-600 transition flex items-center gap-1.5">
                <i class="pi pi-refresh"></i> Retry
              </button>
            </div>
          </div>
        </div>
      }

      <!-- Content -->
      @else if (details(); as d) {
        <div class="animate-fade-in">

          <!-- Back + Breadcrumb -->
          <div class="flex items-center gap-2 mb-6 min-w-0">
            <button
              type="button"
              (click)="goBack()"
              class="text-sm font-semibold text-slate-500 hover:text-slate-700 flex items-center gap-1.5 shrink-0">
              <i class="pi pi-arrow-left text-xs"></i>
              Submissions
            </button>
            <i class="pi pi-chevron-right text-slate-300 text-xs shrink-0"></i>
            <span class="text-sm text-slate-500 truncate capitalize">{{ patientNameDisplay() || 'Unnamed patient' }}</span>
          </div>

          <!-- Header grid -->
          <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-5">

            <!-- Patient Card -->
            <div class="lg:col-span-2 bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
              <div class="p-5 sm:p-6">
                <div class="flex items-start gap-4">
                  <!-- Patient avatar -->
                  <div class="w-14 h-14 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center shrink-0 text-lg font-bold">
                    {{ getInitials(patientNameDisplay()) }}
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="flex items-start justify-between gap-3 flex-wrap">
                      <h2 class="text-xl font-bold text-slate-900 m-0 break-words capitalize">{{ patientNameDisplay() || 'Unnamed patient' }}</h2>
                      <span class="text-[11px] font-semibold px-2.5 py-1 rounded-full shrink-0" [ngClass]="getStatusPillClass(d.status)">
                        {{ getStatusLabel(d.status) }}
                      </span>
                    </div>
                    @if (patientEmailDisplay()) {
                      <p class="text-sm text-slate-500 mt-1 m-0 flex items-center gap-1.5">
                        <i class="pi pi-envelope text-xs"></i>{{ patientEmailDisplay() }}
                      </p>
                    }
                    @if (patientPhoneDisplay()) {
                      <p class="text-sm text-slate-500 mt-0.5 m-0 flex items-center gap-1.5">
                        <i class="pi pi-phone text-xs"></i>{{ patientPhoneDisplay() }}
                      </p>
                    }
                  </div>
                </div>

                <div class="grid grid-cols-2 gap-4 mt-5 pt-5 border-t border-slate-100">
                  <div class="detail-field">
                    <p class="detail-label">Submitted</p>
                    <p class="detail-value">{{ d.submittedAt | date:'MMM d, y, h:mm a' }}</p>
                  </div>
                  <div class="detail-field col-span-2">
                    <p class="detail-label">Submission ID</p>
                    <p class="detail-value font-mono text-xs break-all select-all">{{ d.id }}</p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Form Details Card -->
            <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-5 sm:p-6">
              <h3 class="text-sm font-bold text-slate-800 uppercase tracking-wider mb-4 flex items-center gap-2">
                <i class="pi pi-file-edit text-indigo-500 text-xs"></i>
                Form Details
              </h3>
              <div class="space-y-3">
                <div class="detail-field">
                  <p class="detail-label">Form Name</p>
                  <p class="detail-value">{{ d.formSchemaName }}</p>
                </div>
                <div class="detail-field">
                  <p class="detail-label">Version</p>
                  <span class="inline-flex items-center px-2 py-0.5 rounded bg-slate-100 text-slate-600 text-xs font-medium">
                    v{{ d.formSchemaVersion }}
                  </span>
                </div>
                @if (d.reviewedAt) {
                  <div class="detail-field">
                    <p class="detail-label">Reviewed At</p>
                    <p class="detail-value">{{ d.reviewedAt | date:'MMM d, y' }}</p>
                  </div>
                }
                @if (d.convertedToPatientId) {
                  <div class="detail-field">
                    <p class="detail-label">Patient ID</p>
                    <p class="detail-value font-mono text-xs break-all">{{ d.convertedToPatientId }}</p>
                  </div>
                }
              </div>
            </div>
          </div>

          <!-- Submitted Answers -->
          <div class="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden mb-4">
            <div class="px-5 sm:px-6 py-4 border-b border-slate-100 flex items-center gap-2">
              <i class="pi pi-list text-indigo-500 text-sm"></i>
              <h3 class="text-sm font-bold text-slate-800 uppercase tracking-wider m-0">Submitted Answers</h3>
              @if (isEditing()) {
                <span class="ml-1 text-[11px] font-semibold px-2 py-0.5 rounded-full bg-indigo-50 text-indigo-600">Editing</span>
              }
            </div>

            @if (isEditing()) {
              @if (schema()) {
                <div class="p-5 sm:p-6">
                  <app-dynamic-form-renderer
                    [schema]="schema()"
                    [formSchemaId]="d.formSchemaId"
                    [formSchemaVersion]="d.formSchemaVersion"
                    [initialAnswers]="initialAnswersForEdit()"
                    (submissionChange)="editedSubmission.set($event)"
                    (validityChange)="editIsValid.set($event)" />
                </div>
              } @else {
                <div class="p-5 sm:p-6 text-sm text-slate-500">Form schema unavailable — can't edit answers without it.</div>
              }
            } @else if (submissionData(); as data) {
              <div class="p-5 sm:p-6 space-y-5">
                @for (section of data.sections; track section.sectionId) {
                  <div>
                    <h4 class="text-sm font-semibold text-slate-700 mb-3 flex items-center gap-2">
                      <div class="w-1.5 h-4 rounded-full bg-indigo-500"></div>
                      {{ getSectionTitle(section.sectionId) || 'Section' }}
                    </h4>
                    @for (group of section.groups; track group.groupId) {
                      <div class="ml-4 mb-3 rounded-xl border border-slate-100 overflow-hidden">
                        <div class="bg-slate-50 px-3 py-2 border-b border-slate-100">
                          <p class="text-xs font-semibold text-slate-500 m-0">{{ getGroupTitle(group.groupId) || 'Group' }}</p>
                        </div>
                        <div class="p-3 grid grid-cols-1 sm:grid-cols-2 gap-3">
                          @for (answer of group.answers; track answer.questionId) {
                            <div class="answer-field">
                              <p class="answer-label">{{ getQuestionText(answer.questionId) || 'Question' }}</p>
                              <p class="answer-value">{{ formatAnswerValue(answer) }}</p>
                            </div>
                          }
                        </div>
                      </div>
                    }
                  </div>
                }
              </div>
            } @else {
              <div class="text-center py-10">
                <i class="pi pi-file text-slate-300 text-3xl mb-2"></i>
                <p class="text-sm text-slate-400">No submitted answers available.</p>
              </div>
            }
          </div>

          <!-- Pain Map -->
          @if (isEditing() || hasPainData()) {
            <app-body-pain-map
              [readOnly]="!isEditing()"
              [showDoctorFields]="isEditing()"
              [initialValue]="painMapPayload()"
              (mapChange)="editedPainMap.set($event)" />
          }

          <!-- Actions -->
          <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-4 sm:p-5 mt-4">
            <h3 class="text-xs font-bold text-slate-500 uppercase tracking-wider mb-3">Actions</h3>

            @if (!isEditing()) {
              <div class="flex flex-wrap justify-center gap-2">
                @for (action of availableActions(); track action.type + action.status) {
                  <button
                    type="button"
                    (click)="confirmUpdate(action)"
                    [disabled]="updating()"
                    class="!rounded-full py-2 px-4 text-sm font-semibold disabled:opacity-60 transition-colors duration-150 inline-flex items-center gap-1.5 whitespace-nowrap"
                    [ngClass]="getActionButtonClass(action.severity)"
                    [attr.aria-label]="action.label">
                    <i class="pi text-xs" [ngClass]="updating() ? 'pi-spin pi-spinner' : action.icon"></i>
                    {{ action.label }}
                  </button>
                }
                @if (canEdit()) {
                  <button
                    type="button"
                    (click)="startEditing()"
                    class="!rounded-full py-2 px-4 text-sm font-semibold border border-slate-200 text-slate-600 bg-white hover:bg-slate-100 hover:border-slate-300 transition-colors duration-150 inline-flex items-center gap-1.5 whitespace-nowrap"
                    aria-label="Edit submission">
                    <i class="pi pi-pencil text-xs"></i>
                    Edit
                  </button>
                }
              </div>
            } @else {
              <!-- Editing now leads straight into conversion — there's no separate
                   approve step. The doctor fixes the data, then converts the intake
                   into a patient record directly from here. -->
              <div class="flex flex-wrap justify-center gap-2">
                <button
                  type="button"
                  (click)="cancelEditing()"
                  class="!rounded-full py-2 px-4 text-sm font-semibold border border-slate-200 text-slate-600 bg-white hover:bg-slate-100 hover:border-slate-300 transition-colors duration-150 whitespace-nowrap"
                  aria-label="Cancel editing">
                  Cancel
                </button>
                <button
                  type="button"
                  (click)="showConvertDialog.set(true)"
                  [disabled]="!editIsValid() || updating()"
                  class="!rounded-full py-2 px-4 text-sm font-semibold bg-emerald-600 text-white hover:bg-emerald-700 shadow-sm disabled:opacity-60 transition-colors duration-150 inline-flex items-center gap-1.5 whitespace-nowrap"
                  aria-label="Convert to patient">
                  <i class="pi text-xs" [ngClass]="updating() ? 'pi-spin pi-spinner' : 'pi-user-plus'"></i>
                  Convert to Patient
                </button>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }

    .detail-field {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .detail-label {
      font-size: 0.6875rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #94a3b8;
      margin: 0;
    }

    .detail-value {
      font-size: 0.875rem;
      color: #334155;
      margin: 0;
    }

    .answer-field {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
      padding: 0.5rem 0;
      border-bottom: 1px solid #f1f5f9;
    }

    .answer-label {
      font-size: 0.6875rem;
      color: #94a3b8;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      font-weight: 600;
      margin: 0;
    }

    .answer-value {
      font-size: 0.875rem;
      font-weight: 500;
      color: #1e293b;
      margin: 0;
    }
  `]
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

  /** Soft pill colors matching the rest of the app (submission-list uses the same palette). */
  getStatusPillClass(status: IntakeStatus): string {
    switch (status) {
      case IntakeStatus.Pending:
      case IntakeStatus.Submitted:
      case IntakeStatus.InReview:
        return 'bg-amber-50 text-amber-700';
      case IntakeStatus.Approved:
      case IntakeStatus.Converted:
        return 'bg-emerald-50 text-emerald-700';
      case IntakeStatus.Rejected:
      case IntakeStatus.Expired:
        return 'bg-rose-50 text-rose-700';
      default:
        return 'bg-slate-100 text-slate-600';
    }
  }

  /** Matches the reference design: solid fill for positive/primary actions,
   *  white background with a colored border for destructive or informational ones —
   *  not soft-pastel fills, this is the dedicated bottom action bar. */
  getActionButtonClass(severity: string): string {
    switch (severity) {
      case 'success':
      case 'warn':
        return 'bg-emerald-600 text-white hover:bg-emerald-700 shadow-sm';
      case 'danger':
        return 'bg-white text-rose-600 border border-rose-200 hover:bg-rose-50';
      case 'info':
        return 'bg-white text-indigo-600 border border-indigo-200 hover:bg-indigo-50';
      default:
        return 'bg-white text-slate-600 border border-slate-200 hover:bg-slate-50';
    }
  }
}