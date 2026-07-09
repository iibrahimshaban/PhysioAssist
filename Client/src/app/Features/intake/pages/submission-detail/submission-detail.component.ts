import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { DrawerModule } from 'primeng/drawer';
import { ConfirmationService } from 'primeng/api';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IntakeApiService } from '../../services/intake-api.service';
import { DynamicFormEngineService } from '../../services/dynamic-form-engine.service';
import { BodySvgComponent } from '../../components/body-svg/body-svg.component';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import {
  PreVisitIntakeDetailsResponse,
  DynamicFormSchemaDto,
  DynamicFormSubmissionDto,
  IntakeStatus,
  PainPointDto,
  SubmissionAnswerDto,
  FormQuestionDto,
  UpdateIntakeStatusRequest,
} from '../../models';

@Component({
  selector: 'app-submission-detail',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    TagModule,
    TextareaModule,
    ConfirmDialogModule,
    DialogModule,
    DrawerModule,
    BodySvgComponent,
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
          <div class="w-9 h-9 rounded-lg flex items-center justify-center"
               style="background: linear-gradient(135deg, #22c55e, #16a34a);">
            <i class="pi pi-user-plus text-white text-sm"></i>
          </div>
          <div>
            <h3 class="font-bold text-base m-0">Convert to Patient</h3>
            <p class="text-xs text-surface-500 m-0">Create a patient record from this intake</p>
          </div>
        </div>
      </ng-template>

      <div class="space-y-4">
        <p class="text-sm text-surface-600">
          This will create a new patient record from this intake submission. Review the details below and confirm.
        </p>
        <div>
          <label class="block text-sm font-semibold text-surface-700 mb-1.5">Notes (optional)</label>
          <textarea
            pTextarea
            [(ngModel)]="convertNotes"
            placeholder="Add any notes about this patient..."
            rows="3"
            class="w-full">
          </textarea>
        </div>
        <div class="rounded-xl border border-surface-200 overflow-hidden">
          <div class="bg-surface-50 px-4 py-2 border-b border-surface-100">
            <p class="text-xs font-semibold text-surface-500 uppercase tracking-wider m-0">Patient Details</p>
          </div>
          <div class="p-4 space-y-3">
            <div>
              <p class="text-xs text-surface-400 uppercase tracking-wide m-0">Name</p>
              <p class="text-sm font-semibold text-surface-800 m-0">{{ details()?.patientName }}</p>
            </div>
            @if (details()?.patientEmail) {
              <div>
                <p class="text-xs text-surface-400 uppercase tracking-wide m-0">Email</p>
                <p class="text-sm text-surface-700 m-0">{{ details()?.patientEmail }}</p>
              </div>
            }
            @if (details()?.patientPhone) {
              <div>
                <p class="text-xs text-surface-400 uppercase tracking-wide m-0">Phone</p>
                <p class="text-sm text-surface-700 m-0">{{ details()?.patientPhone }}</p>
              </div>
            }
          </div>
        </div>
      </div>
      <ng-template pTemplate="footer">
        <div class="flex gap-2 justify-end">
          <p-button
            label="Cancel"
            severity="secondary"
            [outlined]="true"
            (onClick)="showConvertDialog.set(false)"
            aria-label="Cancel conversion" />
          <p-button
            label="Confirm Conversion"
            icon="pi pi-user-plus"
            severity="success"
            (onClick)="convertToPatient()"
            [loading]="updating()"
            aria-label="Confirm convert to patient" />
        </div>
      </ng-template>
    </p-dialog>

    <div class="page-container" aria-live="polite">

      <!-- Loading State -->
      @if (loading()) {
        <div class="flex flex-col items-center justify-center py-24 animate-fade-in" role="status">
          <div class="premium-spinner mb-4"></div>
          <p class="text-surface-500 text-sm">Loading submission details...</p>
        </div>
      }

      <!-- Error State -->
      @else if (error()) {
        <div class="max-w-lg mx-auto mt-8 animate-fade-in-up">
          <div class="bg-white rounded-2xl shadow-sm border border-red-100 p-8 text-center" role="alert">
            <div class="w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4"
                 style="background: #fef2f2;">
              <i class="pi pi-exclamation-triangle text-3xl text-red-400"></i>
            </div>
            <h2 class="text-lg font-semibold text-surface-800 mb-2">Failed to Load</h2>
            <p class="text-surface-600 text-sm mb-6">{{ error() }}</p>
            <div class="flex gap-3 justify-center">
              <p-button label="Go Back" icon="pi pi-arrow-left" severity="secondary" [outlined]="true" (onClick)="goBack()" />
              <p-button label="Retry" icon="pi pi-refresh" severity="warn" (onClick)="loadDetails()" />
            </div>
          </div>
        </div>
      }

      <!-- Content -->
      @else if (details(); as d) {
        <div class="max-w-4xl mx-auto animate-fade-in">

          <!-- Back + Breadcrumb -->
          <div class="flex items-center gap-2 mb-5">
            <p-button
              label="Submissions"
              icon="pi pi-arrow-left"
              [text]="true"
              severity="secondary"
              (onClick)="goBack()"
              styleClass="!px-2" />
            <i class="pi pi-chevron-right text-surface-300 text-xs"></i>
            <span class="text-sm text-surface-500 truncate">{{ d.patientName }}</span>
          </div>

          <!-- Header grid -->
          <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-5">

            <!-- Patient Card -->
            <div class="lg:col-span-2">
              <p-card>
                <ng-template pTemplate="header">
                  <div class="p-5 pb-0">
                    <div class="flex items-start gap-4">
                      <!-- Patient avatar -->
                      <div class="w-14 h-14 rounded-2xl flex items-center justify-center shrink-0 text-lg font-bold text-white"
                           [style.background]="getAvatarGradient(d.patientName)">
                        {{ getInitials(d.patientName) }}
                      </div>
                      <div class="flex-1 min-w-0">
                        <div class="flex items-start justify-between gap-3">
                          <h2 class="text-xl font-bold text-surface-900 m-0 break-words">{{ d.patientName }}</h2>
                          <p-tag
                            [value]="getStatusLabel(d.status)"
                            [severity]="getStatusSeverity(d.status)"
                            class="shrink-0" />
                        </div>
                        @if (d.patientEmail) {
                          <p class="text-sm text-surface-500 mt-1 m-0">
                            <i class="pi pi-envelope text-xs mr-1"></i>{{ d.patientEmail }}
                          </p>
                        }
                        @if (d.patientPhone) {
                          <p class="text-sm text-surface-500 mt-0.5 m-0">
                            <i class="pi pi-phone text-xs mr-1"></i>{{ d.patientPhone }}
                          </p>
                        }
                      </div>
                    </div>
                  </div>
                </ng-template>

                <div class="px-5 py-4">
                  <div class="grid grid-cols-2 gap-4">
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

                <ng-template pTemplate="footer">
                  @if (availableActions().length > 0) {
                    <div class="flex flex-wrap gap-2">
                      @for (action of availableActions(); track action.type + action.status) {
                        <p-button
                          [label]="action.label"
                          [icon]="action.icon"
                          [severity]="action.severity"
                          (onClick)="confirmUpdate(action)"
                          [loading]="updating()"
                          [ariaLabel]="action.label" />
                      }
                    </div>
                  }
                </ng-template>
              </p-card>
            </div>

            <!-- Form Details Card -->
            <div>
              <p-card>
                <ng-template pTemplate="header">
                  <div class="px-5 pt-4 pb-2">
                    <h3 class="text-sm font-semibold text-surface-700 m-0 flex items-center gap-2">
                      <i class="pi pi-file-edit text-surface-400 text-xs"></i>
                      Form Details
                    </h3>
                  </div>
                </ng-template>
                <div class="px-5 pb-4 space-y-3">
                  <div class="detail-field">
                    <p class="detail-label">Form Name</p>
                    <p class="detail-value">{{ d.formSchemaName }}</p>
                  </div>
                  <div class="detail-field">
                    <p class="detail-label">Version</p>
                    <span class="inline-flex items-center px-2 py-0.5 rounded bg-surface-100 text-surface-600 text-xs font-medium">
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
              </p-card>
            </div>
          </div>

          <!-- Submitted Answers -->
          <p-card class="mb-4">
            <ng-template pTemplate="header">
              <div class="px-5 py-4 border-b border-surface-100 flex items-center gap-2">
                <i class="pi pi-list text-surface-400 text-sm"></i>
                <h3 class="text-base font-semibold text-surface-800 m-0">Submitted Answers</h3>
              </div>
            </ng-template>

            @if (submissionData(); as data) {
              <div class="p-5 space-y-5">
                @for (section of data.sections; track section.sectionId) {
                  <div>
                    <h4 class="text-sm font-semibold text-surface-700 mb-3 flex items-center gap-2">
                      <div class="w-1.5 h-4 rounded-full" style="background: linear-gradient(135deg, #6366f1, #8b5cf6);"></div>
                      {{ getSectionTitle(section.sectionId) || 'Section' }}
                    </h4>
                    @for (group of section.groups; track group.groupId) {
                      <div class="ml-4 mb-3 rounded-xl border border-surface-100 overflow-hidden">
                        <div class="bg-surface-50 px-3 py-2 border-b border-surface-100">
                          <p class="text-xs font-semibold text-surface-500 m-0">{{ getGroupTitle(group.groupId) || 'Group' }}</p>
                        </div>
                        <div class="p-3 grid grid-cols-1 sm:grid-cols-2 gap-3">
                          @for (answer of group.answers; track answer.questionId) {
                            <div class="answer-field">
                              <p class="answer-label">{{ getQuestionText(answer.questionId) || 'Question' }}</p>
                              <p class="answer-value">{{ formatAnswerValue(answer) }}</p>
                              @if (answer.notes) {
                                <p class="text-xs text-surface-400 italic mt-0.5">Note: {{ answer.notes }}</p>
                              }
                            </div>
                          }
                        </div>
                      </div>
                    }
                  </div>
                }
              </div>
            } @else {
              <div class="empty-state py-8">
                <i class="pi pi-file text-surface-300 text-3xl mb-2"></i>
                <p class="empty-state-text">No submitted answers available.</p>
              </div>
            }
          </p-card>

          <!-- Pain Points -->
          @if (painPoints().length > 0) {
            <p-card class="mb-4">
              <ng-template pTemplate="header">
                <div class="px-5 py-4 border-b border-surface-100 flex items-center gap-2">
                  <i class="pi pi-map-marker text-red-400 text-sm"></i>
                  <h3 class="text-base font-semibold text-surface-800 m-0">Pain Points</h3>
                  <span class="ml-1 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-red-50 text-red-600">
                    {{ painPoints().length }} marked
                  </span>
                </div>
              </ng-template>

              <div class="p-5">
                <div class="flex flex-col sm:flex-row gap-6 items-start">
                  <div class="w-44 sm:w-52 shrink-0 mx-auto sm:mx-0">
                    <app-body-svg
                      [view]="painPointView()"
                      [points]="painPoints()"
                      (viewChange)="painPointView.set($event)" />
                  </div>
                  <div class="flex-1 w-full">
                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                      @for (point of painPoints(); track $index) {
                        <div class="flex items-center gap-3 p-3 border border-surface-200 rounded-xl bg-white hover:border-surface-300 transition-all">
                          <div class="w-8 h-8 rounded-full flex items-center justify-center shrink-0 text-white text-xs font-bold"
                               [style.background]="getIntensityGradient(point.intensity)">
                            {{ point.intensity }}
                          </div>
                          <div>
                            <p class="text-xs font-medium text-surface-700 m-0">
                              {{ point.bodyPart === 'back' ? 'Back' : 'Front' }}
                            </p>
                            <p class="text-xs text-surface-400 m-0">
                              Intensity {{ point.intensity }}/10
                              <span class="ml-1 inline-block" [style.color]="getIntensityColor(point.intensity)">
                                {{ getIntensityLabel(point.intensity) }}
                              </span>
                            </p>
                          </div>
                        </div>
                      }
                    </div>
                  </div>
                </div>
              </div>
            </p-card>
          }
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
  convertNotes = '';
  readonly details = signal<PreVisitIntakeDetailsResponse | null>(null);
  readonly schema = signal<DynamicFormSchemaDto | null>(null);
  readonly submissionData = signal<DynamicFormSubmissionDto | null>(null);
  readonly painPoints = signal<PainPointDto[]>([]);
  readonly painPointView = signal<'front' | 'back'>('front');

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
          status: IntakeStatus.Approved,
          label: 'Approve',
          icon: 'pi pi-check-circle',
          severity: 'warn' as const,
          message: 'Approve this submission?'
        },
        {
          type: 'status',
          status: IntakeStatus.Rejected,
          label: 'Reject',
          icon: 'pi pi-times-circle',
          severity: 'danger' as const,
          message: 'Reject this submission?'
        }];
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
          status: IntakeStatus.Approved,
          label: 'Approve',
          icon: 'pi pi-check-circle',
          severity: 'warn' as const,
          message: 'Approve this submission?'
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
          status: IntakeStatus.Approved,
          label: 'Approve',
          icon: 'pi pi-check-circle',
          severity: 'warn' as const,
          message: 'Approve this submission?'
        },
        {
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
        },
        {
          type: 'status',
          status: IntakeStatus.Approved,
          label: 'Re-approve',
          icon: 'pi pi-check-circle',
          severity: 'warn' as const,
          message: 'Re-approve this submission?'
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
      this.error.set('Invalid submission ID.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

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
            this.painPoints.set(JSON.parse(data.painPointsData) as PainPointDto[]);
          } catch {
            // Pain points parsing failed
          }
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
      acceptButtonStyleClass: 'p-button-' + action.severity,
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

    this.intakeApi.convertToPatient(id, { notes: this.convertNotes || undefined }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.showConvertDialog.set(false);
        this.convertNotes = '';
        this.snackbar.success('Conversion Successful', ['Submission has been converted to a patient record.']);
        this.updating.set(false);
        this.loadDetails();
      },
      error: (err: any) => {
        this.updating.set(false);
        const msg = err?.error?.detail || err?.error?.title || 'Could not convert submission to patient.';
        this.snackbar.error('Conversion Failed', [msg]);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/intake/submissions']);
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getAvatarGradient(name: string): string {
    const gradients = [
      'linear-gradient(135deg, #6366f1, #8b5cf6)',
      'linear-gradient(135deg, #ec4899, #f43f5e)',
      'linear-gradient(135deg, #14b8a6, #06b6d4)',
      'linear-gradient(135deg, #f59e0b, #ef4444)',
      'linear-gradient(135deg, #22c55e, #16a34a)',
    ];
    return gradients[name.charCodeAt(0) % gradients.length];
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

  getStatusSeverity(status: IntakeStatus): 'info' | 'warn' | 'success' | 'danger' | 'secondary' | 'contrast' {
    switch (status) {
      case IntakeStatus.Pending: return 'info';
      case IntakeStatus.Submitted: return 'info';
      case IntakeStatus.InReview: return 'warn';
      case IntakeStatus.Approved: return 'success';
      case IntakeStatus.Rejected: return 'secondary';
      case IntakeStatus.Converted: return 'success';
      case IntakeStatus.Expired: return 'secondary';
      default: return 'info';
    }
  }

  getIntensityColor(intensity: number): string {
    if (intensity <= 3) return '#22c55e';
    if (intensity <= 6) return '#f59e0b';
    return '#ef4444';
  }

  getIntensityGradient(intensity: number): string {
    if (intensity <= 3) return 'linear-gradient(135deg, #22c55e, #16a34a)';
    if (intensity <= 6) return 'linear-gradient(135deg, #f59e0b, #d97706)';
    return 'linear-gradient(135deg, #ef4444, #dc2626)';
  }

  getIntensityLabel(intensity: number): string {
    if (intensity <= 3) return 'Mild';
    if (intensity <= 6) return 'Moderate';
    return 'Severe';
  }
}
