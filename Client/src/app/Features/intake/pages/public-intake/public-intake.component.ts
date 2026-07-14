import { Component, computed, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { QrAccessService } from '../../services/qr-access.service';
import { DynamicFormEngineService } from '../../services/dynamic-form-engine.service';
import { DynamicFormRendererComponent } from '../../components/dynamic-form-renderer/dynamic-form-renderer.component';
import {
  PublicIntakeFormResponse,
  PublicIntakeSubmissionResponse,
  DynamicFormSchemaDto,
  DynamicFormSubmissionDto,
  SubmitPreVisitIntakeRequest,
  PainPointDto
} from '../../models';

@Component({
  selector: 'app-public-intake',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    InputTextModule,
    DynamicFormRendererComponent
  ],
  template: `
    <div class="min-h-screen bg-slate-50/50 flex flex-col font-sans" role="main" aria-label="Intake form page">
      <!-- ── Branded Header ──────────────────────────────────────── -->
      <header class="bg-white border-b border-slate-100 sticky top-0 z-50 backdrop-blur-md bg-white/90">
        <div class="max-w-3xl mx-auto px-4 h-16 flex items-center justify-between">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-indigo-500 to-indigo-600 flex items-center justify-center text-white">
              <i class="pi pi-heart-fill text-sm"></i>
            </div>
            <span class="text-base font-bold text-slate-900 tracking-tight">Physio<span class="text-indigo-600">Assist</span></span>
          </div>
          <span class="text-xs font-semibold text-indigo-600 bg-indigo-50 px-2.5 py-1 rounded-full">
            Patient Portal
          </span>
        </div>
      </header>

      <!-- ── Loading State ────────────────────────────────────────── -->
      @if (loading()) {
        <div class="flex-1 flex flex-col items-center justify-center py-24 px-4 animate-fade-in" role="status" aria-label="Loading">
          <div class="premium-spinner mb-4"></div>
          <p class="text-slate-500 font-medium animate-pulse text-sm">Securing your connection & loading form...</p>
        </div>
      }

      <!-- ── Error State ──────────────────────────────────────────── -->
      @if (error()) {
        <div class="max-w-lg mx-auto mt-12 px-4 w-full animate-fade-in-up">
          <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-8 text-center" role="alert">
            <div class="w-16 h-16 rounded-full bg-rose-50 flex items-center justify-center mx-auto mb-5 text-rose-500">
              <i class="pi pi-exclamation-triangle text-3xl" aria-hidden="true"></i>
            </div>
            <h2 class="text-lg font-bold text-slate-800 mb-2">Unable to Load Intake Form</h2>
            <p class="text-slate-600 mb-6 text-sm leading-relaxed">{{ error() }}</p>
            <div class="flex flex-col sm:flex-row gap-3 justify-center">
              <p-button
                label="Try Again"
                icon="pi pi-refresh"
                (onClick)="retry()"
                severity="primary"
                styleClass="w-full sm:w-auto">
              </p-button>
              <p-button
                label="Go Back Home"
                icon="pi pi-home"
                (onClick)="goHome()"
                [outlined]="true"
                severity="secondary"
                styleClass="w-full sm:w-auto">
              </p-button>
            </div>
          </div>
        </div>
      }

      <!-- ── Success State ────────────────────────────────────────── -->
      @if (submitted() && submissionResult()) {
        <div class="max-w-lg mx-auto mt-12 px-4 w-full animate-fade-in-up">
          <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-8 text-center" role="status" aria-label="Form submitted successfully">
            <div class="w-20 h-20 bg-emerald-50 rounded-full flex items-center justify-center mx-auto mb-6">
              <svg class="w-10 h-10 text-emerald-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
                <path stroke-linecap="round" stroke-linejoin="round" class="success-checkmark-check" d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h2 class="text-xl font-bold text-slate-900 mb-2">Thank you!</h2>
            <p class="text-slate-500 mb-6 text-sm">{{ submissionResult()!.message }}</p>
            
            <div class="bg-slate-50 rounded-xl p-4 mb-6 border border-slate-100 text-left">
              <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1">Receipt ID</p>
              <p class="text-xs font-mono text-slate-800 break-all select-all font-semibold">{{ submissionResult()!.submissionId }}</p>
            </div>
            
            <p class="text-[11px] text-slate-400">
              Submitted at {{ submissionResult()!.submittedAt | date:'medium' }}
            </p>
          </div>
        </div>
      }

      <!-- ── Form View ────────────────────────────────────────────── -->
      @if (formData() && schema() && !submitted()) {
        <div class="max-w-3xl mx-auto py-8 px-4 w-full flex-1 flex flex-col justify-between">
          <div>
            <!-- Banner / Header Info -->
            <div class="mb-8">
              <h1 class="text-2xl font-extrabold text-slate-900 tracking-tight">{{ formData()!.formName }}</h1>
              @if (formData()!.formDescription) {
                <p class="text-slate-500 mt-2 text-sm leading-relaxed">{{ formData()!.formDescription }}</p>
              }
            </div>

            <!-- Patient Demographics Section -->
            <section class="bg-white rounded-2xl shadow-sm border border-slate-100 p-5 sm:p-6 mb-6" aria-label="Patient information">
              <h2 class="text-sm font-bold text-slate-800 uppercase tracking-wider mb-4 flex items-center gap-2">
                <i class="pi pi-user text-indigo-500 text-xs"></i>
                Patient Details
              </h2>
              
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div class="sm:col-span-2">
                  <label for="patient-name" class="block text-xs font-bold text-slate-600 uppercase tracking-wider mb-1.5">
                    Full Name <span class="text-rose-500" aria-label="required">*</span>
                  </label>
                  <input
                    id="patient-name"
                    type="text"
                    pInputText
                    [ngModel]="patientName()"
                    (ngModelChange)="patientName.set($event)"
                    placeholder="e.g. John Doe"
                    class="w-full"
                    [attr.aria-required]="true"
                    [attr.aria-invalid]="nameTouched() && patientName().trim().length === 0"
                    [class.ng-invalid]="nameTouched() && patientName().trim().length === 0"
                    (blur)="nameTouched.set(true)" />
                  @if (nameTouched() && patientName().trim().length === 0) {
                    <p class="text-xs text-rose-500 mt-1.5 font-medium flex items-center gap-1" role="alert">
                      <i class="pi pi-exclamation-circle text-xs"></i>
                      Full name is required to submit your intake.
                    </p>
                  }
                </div>
                
                <div>
                  <label for="patient-email" class="block text-xs font-bold text-slate-600 uppercase tracking-wider mb-1.5">Email Address</label>
                  <input
                    id="patient-email"
                    type="email"
                    pInputText
                    [ngModel]="patientEmail()"
                    (ngModelChange)="patientEmail.set($event)"
                    placeholder="john@example.com"
                    class="w-full" />
                </div>
                
                <div>
                  <label for="patient-phone" class="block text-xs font-bold text-slate-600 uppercase tracking-wider mb-1.5">Phone Number</label>
                  <input
                    id="patient-phone"
                    type="tel"
                    pInputText
                    [ngModel]="patientPhone()"
                    (ngModelChange)="patientPhone.set($event)"
                    placeholder="(555) 000-0000"
                    class="w-full" />
                </div>
              </div>
            </section>

            <!-- Dynamic Fields Render -->
            <app-dynamic-form-renderer
              [schema]="schema()"
              [formSchemaId]="formData()!.formSchemaId"
              [formSchemaVersion]="formData()!.version"
              (submissionChange)="onSubmissionChange($event)"
              (validityChange)="onValidityChange($event)" />

            <!-- Submission Errors -->
            @if (submitError()) {
              <div class="mt-4 p-4 bg-rose-50 border border-rose-100 rounded-2xl" role="alert">
                <p class="text-sm text-rose-800 font-semibold flex items-center gap-2 m-0">
                  <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
                  {{ submitError() }}
                </p>
              </div>
            }
          </div>

          <!-- Submit Buttons -->
          <div class="mt-8 pt-4 border-t border-slate-100 flex flex-col sm:flex-row justify-between items-center gap-4">
            <div class="flex items-center gap-2 text-xs text-slate-400">
              <i class="pi pi-lock text-[10px]"></i>
              <span>Your data is encrypted and secure</span>
            </div>
            <p-button
              label="Submit Form"
              icon="pi pi-check"
              [disabled]="!canSubmit()"
              [loading]="submitting()"
              (onClick)="submit()"
              severity="primary"
              size="large"
              styleClass="w-full sm:w-auto shadow-sm">
            </p-button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
  `]
})
export class PublicIntakeComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly qrAccessService = inject(QrAccessService);
  private readonly dynamicFormEngine = inject(DynamicFormEngineService);
  private readonly destroyRef = inject(DestroyRef);

  private token: string | null = null;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly formData = signal<PublicIntakeFormResponse | null>(null);
  readonly schema = signal<DynamicFormSchemaDto | null>(null);

  readonly patientName = signal('');
  readonly patientEmail = signal('');
  readonly patientPhone = signal('');
  readonly nameTouched = signal(false);

  readonly submission = signal<DynamicFormSubmissionDto | null>(null);
  readonly isFormValid = signal(false);
  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly submissionResult = signal<PublicIntakeSubmissionResponse | null>(null);
  readonly submitError = signal<string | null>(null);

  readonly canSubmit = computed(() =>
    this.patientName().trim().length > 0
    && this.isFormValid()
    && !this.submitting()
    && !this.submitted()
  );

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const token = params.get('token');
      this.loadForm(token);
    });
  }

  goHome(): void {
    window.location.href = '/';
  }

  retry(): void {
    this.loadForm(this.token);
  }

  onSubmissionChange(submission: DynamicFormSubmissionDto): void {
    this.submission.set(submission);
  }

  onValidityChange(valid: boolean): void {
    this.isFormValid.set(valid);
  }

  submit(): void {
    const currentSubmission = this.submission();
    const currentSchema = this.schema();
    if (!currentSubmission || !currentSchema) return;

    this.submitting.set(true);
    this.submitError.set(null);

    const request: SubmitPreVisitIntakeRequest = {
      patientName: this.patientName().trim(),
      patientEmail: this.patientEmail().trim() || undefined,
      patientPhone: this.patientPhone().trim() || undefined,
      formSubmissionData: JSON.stringify(currentSubmission)
    };

    const painPoints = this.extractPainPoints(currentSubmission, currentSchema);
    if (painPoints.length > 0) {
      request.painPointsData = JSON.stringify(painPoints);
    }

    this.qrAccessService.submitPublicIntake(this.token!, request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        this.submissionResult.set(response);
        this.submitted.set(true);
        this.submitting.set(false);
      },
      error: (err) => {
        const detail = err?.error?.detail || err?.error?.title || err?.error?.message;
        this.submitError.set(detail || 'Failed to submit the form. Please try again.');
        this.submitting.set(false);
      }
    });
  }

  private extractPainPoints(
    submission: DynamicFormSubmissionDto,
    schema: DynamicFormSchemaDto
  ): PainPointDto[] {
    const bodySelectorIds = new Set<string>();
    for (const section of schema.sections) {
      for (const group of section.groups) {
        for (const question of group.questions) {
          if (question.type === 'bodyselector') {
            bodySelectorIds.add(question.questionId);
          }
        }
      }
    }

    const painPoints: PainPointDto[] = [];
    for (const section of submission.sections) {
      for (const group of section.groups) {
        for (const answer of group.answers) {
          if (bodySelectorIds.has(answer.questionId) && Array.isArray(answer.value)) {
            painPoints.push(...answer.value);
          }
        }
      }
    }
    return painPoints;
  }

  private loadForm(token: string | null): void {
    this.token = token;

    if (!this.token) {
      this.error.set('Invalid URL: No form token found. Please check that you have the correct link.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.qrAccessService.getPublicForm(this.token).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        try {
          const parsedSchema = this.dynamicFormEngine.deserializeSchema(response.schemaJson);

          if (!parsedSchema?.sections) {
            this.error.set('The form schema appears to be empty or corrupted. Please contact your healthcare provider.');
            this.loading.set(false);
            return;
          }

          this.formData.set(response);
          this.schema.set(parsedSchema);
        } catch {
          this.error.set('Failed to parse the form schema. The form may be corrupted. Please request a new link.');
        }
        this.loading.set(false);
      },
      error: (err) => {
        if (err.status === 404) {
          this.error.set('This form link is invalid or has expired. Please request a new link from your healthcare provider.');
        } else if (err.status === 410) {
          this.error.set('This form has expired and is no longer available. Please request a new link.');
        } else {
          this.error.set('Failed to load the form. Please check your internet connection and try again.');
        }
        this.loading.set(false);
      }
    });
  }
}
