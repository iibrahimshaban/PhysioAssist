import { Component, computed, inject, OnInit, signal, DestroyRef, HostListener } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap, catchError, of, map } from 'rxjs';
import { QrAccessService } from '../../services/qr-access.service';
import { DynamicFormEngineService } from '../../services/dynamic-form-engine.service';
import { DynamicFormRendererComponent } from '../../components/dynamic-form-renderer/dynamic-form-renderer.component';
import {
  PublicIntakeFormResponse,
  PublicIntakeSubmissionResponse,
  DynamicFormSchemaDto,
  DynamicFormSubmissionDto,
  SubmitPreVisitIntakeRequest
} from '../../models';
import { BodyPainMapPayload, BodyPainMapComponent } from '../../components/body-pain-map/body-pain-map.component';

@Component({
  selector: 'app-public-intake',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    DynamicFormRendererComponent,
    BodyPainMapComponent
  ],
  templateUrl: './public-intake.component.html',
  styleUrl: './public-intake.component.css'
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

  readonly submission = signal<DynamicFormSubmissionDto | null>(null);
  readonly isFormValid = signal(false);
  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly submissionResult = signal<PublicIntakeSubmissionResponse | null>(null);
  readonly submitError = signal<string | null>(null);

  readonly showConfirmDialog = signal(false);
  readonly isDirty = signal(false);

  readonly requiredTotal = signal(0);
  readonly requiredCompleted = signal(0);

  readonly emailChecking = signal(false);
  readonly emailDuplicate = signal(false);
  readonly duplicateEmailAddress = signal<string | null>(null);

  readonly progressPercent = computed(() => {
    const total = this.requiredTotal();
    if (total === 0) return 0;
    return Math.round((this.requiredCompleted() / total) * 100);
  });

  readonly canSubmit = computed(() =>
    this.isFormValid()
    && !this.submitting()
    && !this.submitted()
    && !this.emailChecking()
    && !this.emailDuplicate()
  );

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(e: BeforeUnloadEvent): void {
    if (this.isDirty() && !this.submitted()) {
      e.preventDefault();
      e.returnValue = '';
    }
  }

  schemaHasBodySelector(): boolean {
    const schema = this.schema();
    if (!schema) return false;

    for (const section of schema.sections) {
      for (const group of section.groups) {
        for (const question of group.questions) {
          if (question.type === 'bodyselector') {
            return true;
          }
        }
      }
    }

    return false;
  }

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const token = params.get('token');
      this.loadForm(token);
    });

    toObservable(this.submission).pipe(
      takeUntilDestroyed(this.destroyRef),
      debounceTime(600),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
      switchMap(submission => {
        const schema = this.schema();
        if (!schema) {
          this.emailDuplicate.set(false);
          this.duplicateEmailAddress.set(null);
          this.emailChecking.set(false);
          return of(null);
        }
        const email = this.dynamicFormEngine.extractEmailAnswer(schema, submission);
        if (!email) {
          this.emailDuplicate.set(false);
          this.duplicateEmailAddress.set(null);
          this.emailChecking.set(false);
          return of(null);
        }
        this.emailChecking.set(true);
        return this.qrAccessService.checkPatientEmail(email).pipe(
          catchError(() => of({ isRegistered: false })),
          map(result => ({ email, result }))
        );
      })
    ).subscribe(checkResult => {
      if (checkResult === null) return;
      const { email, result } = checkResult;
      this.emailChecking.set(false);
      if (result.isRegistered) {
        this.emailDuplicate.set(true);
        this.duplicateEmailAddress.set(email);
      } else {
        this.emailDuplicate.set(false);
        this.duplicateEmailAddress.set(null);
      }
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
    this.isDirty.set(true);
  }

  onValidityChange(valid: boolean): void {
    this.isFormValid.set(valid);
  }

  onRequiredStatsChange(event: { completed: number; total: number }): void {
    this.requiredCompleted.set(event.completed);
    this.requiredTotal.set(event.total);
  }

  painMapPayload = signal<BodyPainMapPayload | null>(null);

  onPainMapChange(payload: BodyPainMapPayload) {
    this.painMapPayload.set(payload);
    this.isDirty.set(true);
  }

  requestSubmit(): void {
    if (!this.canSubmit()) {
      this.markAllAsTouched();
      return;
    }
    this.showConfirmDialog.set(true);
  }

  cancelSubmit(): void {
    this.showConfirmDialog.set(false);
  }

  confirmSubmit(): void {
    this.showConfirmDialog.set(false);
    this.submit();
  }

  private markAllAsTouched(): void {
    const schema = this.schema();
    if (!schema) return;
    // This triggers validation display on all visible fields
    // The dynamic-form-renderer will handle marking its own controls
  }

  submit(): void {
    const currentSubmission = this.submission();
    const currentSchema = this.schema();
    if (!currentSubmission || !currentSchema) return;

    const email = this.dynamicFormEngine.extractEmailAnswer(currentSchema, currentSubmission);
    if (email && this.emailDuplicate()) {
      this.submitError.set(
        `The email address ${email} is already associated with an existing patient record. ` +
        `Please contact your healthcare provider or use a different email address.`
      );
      return;
    }

    if (email && !this.emailDuplicate() && !this.emailChecking()) {
      this.emailChecking.set(true);
      this.qrAccessService.checkPatientEmail(email).pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of({ isRegistered: false }))
      ).subscribe({
        next: (result) => {
          this.emailChecking.set(false);
          if (result.isRegistered) {
            this.emailDuplicate.set(true);
            this.duplicateEmailAddress.set(email);
            this.submitError.set(
              `The email address ${email} is already associated with an existing patient record. ` +
              `Please contact your healthcare provider or use a different email address.`
            );
          } else {
            this.doSubmit(currentSubmission, currentSchema);
          }
        },
        error: () => {
          this.emailChecking.set(false);
          this.doSubmit(currentSubmission, currentSchema);
        }
      });
    } else {
      this.doSubmit(currentSubmission, currentSchema);
    }
  }

  private doSubmit(currentSubmission: DynamicFormSubmissionDto, currentSchema: DynamicFormSchemaDto): void {
    this.submitting.set(true);
    this.submitError.set(null);

    const painMap = this.painMapPayload();

    const request: SubmitPreVisitIntakeRequest = {
      formSubmissionData: JSON.stringify(currentSubmission),
      painPointsData: painMap && painMap.regions.length > 0
        ? JSON.stringify(painMap)
        : undefined
    };

    this.qrAccessService.submitPublicIntake(this.token!, request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        this.submissionResult.set(response);
        this.submitted.set(true);
        this.submitting.set(false);
        this.isDirty.set(false);
      },
      error: (err) => {
        const detail = err?.error?.detail || err?.error?.title || err?.error?.message;
        this.submitError.set(detail || 'Failed to submit the form. Please try again.');
        this.submitting.set(false);
      }
    });
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
