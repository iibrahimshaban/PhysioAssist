import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
  DestroyRef,
  HostListener,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap, catchError, of, map, forkJoin } from 'rxjs';
import { QrAccessService } from '../../services/qr-access.service';
import { DynamicFormEngineService } from '../../services/dynamic-form-engine.service';
import { DynamicFormRendererComponent } from '../../components/dynamic-form-renderer/dynamic-form-renderer.component';
import {
  PublicIntakeFormResponse,
  PublicIntakeSubmissionResponse,
  DynamicFormSchemaDto,
  DynamicFormSubmissionDto,
  SubmitPreVisitIntakeRequest,
} from '../../models';
import {
  BodyPainMapPayload,
  BodyPainMapComponent,
} from '../../components/body-pain-map/body-pain-map.component';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-public-intake',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    DynamicFormRendererComponent,
    BodyPainMapComponent,
    TranslatePipe,
  ],
  templateUrl: './public-intake.component.html',
  styleUrl: './public-intake.component.css',
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
  private readonly submission$ = toObservable(this.submission);

  readonly showConfirmDialog = signal(false);
  readonly isDirty = signal(false);

  readonly requiredTotal = signal(0);
  readonly requiredCompleted = signal(0);

  readonly emailChecking = signal(false);
  readonly emailDuplicate = signal(false);
  readonly duplicateEmailAddress = signal<string | null>(null);

  readonly phoneChecking = signal(false);
  readonly phoneDuplicate = signal(false);
  readonly duplicatePhoneNumber = signal<string | null>(null);

  readonly progressPercent = computed(() => {
    const total = this.requiredTotal();
    if (total === 0) return 0;
    return Math.round((this.requiredCompleted() / total) * 100);
  });

  readonly canSubmit = computed(
    () =>
      this.isFormValid() &&
      !this.submitting() &&
      !this.submitted() &&
      !this.emailChecking() &&
      !this.emailDuplicate() &&
      !this.phoneChecking() &&
      !this.phoneDuplicate(),
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
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const token = params.get('token');
      this.loadForm(token);
    });

    this.submission$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        map((submission) => {
          const schema = this.schema();
          if (!schema || !submission) return null;
          return {
            email: this.dynamicFormEngine.extractEmailAnswer(schema, submission),
            phone: this.dynamicFormEngine.extractPhoneAnswer(schema, submission),
          };
        }),
        debounceTime(600),
        distinctUntilChanged((a, b) => a?.email === b?.email && a?.phone === b?.phone),
      )
      .subscribe((extracted) => {
        this.runEmailCheck(extracted?.email ?? null);
        this.runPhoneCheck(extracted?.phone ?? null);
      });
  }

  private runEmailCheck(email: string | null): void {
    if (!email || !this.token) {
      this.emailDuplicate.set(false);
      this.duplicateEmailAddress.set(null);
      this.emailChecking.set(false);
      return;
    }
    this.emailChecking.set(true);
    this.qrAccessService
      .checkPatientEmail(this.token, email)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of({ isRegistered: false })),
      )
      .subscribe((result) => {
        this.emailChecking.set(false);
        this.emailDuplicate.set(result.isRegistered);
        this.duplicateEmailAddress.set(result.isRegistered ? email : null);
      });
  }

  private runPhoneCheck(phone: string | null): void {
    if (!phone || !this.token) {
      this.phoneDuplicate.set(false);
      this.duplicatePhoneNumber.set(null);
      this.phoneChecking.set(false);
      return;
    }
    this.phoneChecking.set(true);
    this.qrAccessService
      .checkPatientPhone(this.token, phone)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of({ isRegistered: false })),
      )
      .subscribe((result) => {
        this.phoneChecking.set(false);
        this.phoneDuplicate.set(result.isRegistered);
        this.duplicatePhoneNumber.set(result.isRegistered ? phone : null);
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
    if (!currentSubmission || !currentSchema || !this.token) return;

    const email = this.dynamicFormEngine.extractEmailAnswer(currentSchema, currentSubmission);
    const phone = this.dynamicFormEngine.extractPhoneAnswer(currentSchema, currentSubmission);

    if (email && this.emailDuplicate()) {
      this.submitError.set(
        `The email address ${email} is already associated with an existing patient record. ` +
          `Please contact your healthcare provider or use a different email address.`,
      );
      return;
    }
    if (phone && this.phoneDuplicate()) {
      this.submitError.set(
        `The phone number ${phone} is already associated with an existing patient record. ` +
          `Please contact your healthcare provider or use a different phone number.`,
      );
      return;
    }

    this.verifyThenSubmit(email, phone, currentSubmission, currentSchema);
  }

  private verifyThenSubmit(
    email: string | null,
    phone: string | null,
    currentSubmission: DynamicFormSubmissionDto,
    currentSchema: DynamicFormSchemaDto,
  ): void {
    const needsEmailCheck = !!email && !this.emailDuplicate() && !this.emailChecking();
    const needsPhoneCheck = !!phone && !this.phoneDuplicate() && !this.phoneChecking();

    if (!needsEmailCheck && !needsPhoneCheck) {
      this.doSubmit(currentSubmission, currentSchema);
      return;
    }

    if (needsEmailCheck) this.emailChecking.set(true);
    if (needsPhoneCheck) this.phoneChecking.set(true);

    const email$ = needsEmailCheck
      ? this.qrAccessService
          .checkPatientEmail(this.token!, email!)
          .pipe(catchError(() => of({ isRegistered: false })))
      : of(null);

    const phone$ = needsPhoneCheck
      ? this.qrAccessService
          .checkPatientPhone(this.token!, phone!)
          .pipe(catchError(() => of({ isRegistered: false })))
      : of(null);

    forkJoin([email$, phone$])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(([emailResult, phoneResult]) => {
        if (needsEmailCheck) this.emailChecking.set(false);
        if (needsPhoneCheck) this.phoneChecking.set(false);

        const emailTaken = needsEmailCheck && emailResult!.isRegistered;
        const phoneTaken = needsPhoneCheck && phoneResult!.isRegistered;

        if (emailTaken) {
          this.emailDuplicate.set(true);
          this.duplicateEmailAddress.set(email);
        }
        if (phoneTaken) {
          this.phoneDuplicate.set(true);
          this.duplicatePhoneNumber.set(phone);
        }

        if (emailTaken && phoneTaken) {
          this.submitError.set(
            `Both the email address ${email} and the phone number ${phone} are already associated with ` +
              `an existing patient record. Please contact your healthcare provider or use different contact details.`,
          );
          return;
        }
        if (emailTaken) {
          this.submitError.set(
            `The email address ${email} is already associated with an existing patient record. ` +
              `Please contact your healthcare provider or use a different email address.`,
          );
          return;
        }
        if (phoneTaken) {
          this.submitError.set(
            `The phone number ${phone} is already associated with an existing patient record. ` +
              `Please contact your healthcare provider or use a different phone number.`,
          );
          return;
        }

        this.doSubmit(currentSubmission, currentSchema);
      });
  }

  private doSubmit(
    currentSubmission: DynamicFormSubmissionDto,
    currentSchema: DynamicFormSchemaDto,
  ): void {
    this.submitting.set(true);
    this.submitError.set(null);

    const painMap = this.painMapPayload();

    const request: SubmitPreVisitIntakeRequest = {
      formSubmissionData: JSON.stringify(currentSubmission),
      painPointsData: painMap && painMap.regions.length > 0 ? JSON.stringify(painMap) : undefined,
    };

    this.qrAccessService
      .submitPublicIntake(this.token!, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
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
        },
      });
  }

  private loadForm(token: string | null): void {
    this.token = token;

    if (!this.token) {
      this.error.set(
        'Invalid URL: No form token found. Please check that you have the correct link.',
      );
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.qrAccessService
      .getPublicForm(this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          try {
            const parsedSchema = this.dynamicFormEngine.deserializeSchema(response.schemaJson);

            if (!parsedSchema?.sections) {
              this.error.set(
                'The form schema appears to be empty or corrupted. Please contact your healthcare provider.',
              );
              this.loading.set(false);
              return;
            }

            this.formData.set(response);
            this.schema.set(parsedSchema);
          } catch {
            this.error.set(
              'Failed to parse the form schema. The form may be corrupted. Please request a new link.',
            );
          }
          this.loading.set(false);
        },
        error: (err) => {
          if (err.status === 404) {
            this.error.set(
              'This form link is invalid or has expired. Please request a new link from your healthcare provider.',
            );
          } else if (err.status === 410) {
            this.error.set(
              'This form has expired and is no longer available. Please request a new link.',
            );
          } else {
            this.error.set(
              'Failed to load the form. Please check your internet connection and try again.',
            );
          }
          this.loading.set(false);
        },
      });
  }
}
