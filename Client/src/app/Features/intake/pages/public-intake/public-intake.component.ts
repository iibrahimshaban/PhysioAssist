import { Component, computed, inject, OnInit, signal, DestroyRef, HostListener } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap, catchError, of, map } from 'rxjs';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
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
    BodyPainMapComponent,
    TranslocoModule
  ],
  templateUrl: './public-intake.component.html',
  styleUrl: './public-intake.component.css'
})
export class PublicIntakeComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly qrAccessService = inject(QrAccessService);
  private readonly dynamicFormEngine = inject(DynamicFormEngineService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly transloco = inject(TranslocoService);

  readonly currentLang = toSignal(this.transloco.langChanges$, { initialValue: this.transloco.getActiveLang() });

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

    this.submission$.pipe(
      takeUntilDestroyed(this.destroyRef),
      map(submission => {
        const schema = this.schema();
        if (!schema || !submission) return null;
        return this.dynamicFormEngine.extractEmailAnswer(schema, submission);
      }),
      debounceTime(600),
      distinctUntilChanged(),
      switchMap(email => {
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
      this.submitError.set(this.transloco.translate('intake.public.submitErrorAlreadyRegistered', { email }));
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
            this.submitError.set(this.transloco.translate('intake.public.submitErrorAlreadyRegistered', { email }));
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
        this.submitError.set(detail || this.transloco.translate('intake.public.submitErrorGeneric'));
        this.submitting.set(false);
      }
    });
  }


  private loadForm(token: string | null): void {
    this.token = token;

    if (!this.token) {
      this.error.set(this.transloco.translate('intake.public.error.invalidUrl'));
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
            this.error.set(this.transloco.translate('intake.public.error.emptySchema'));
            this.loading.set(false);
            return;
          }

          this.formData.set(response);
          this.schema.set(parsedSchema);
        } catch {
          this.error.set(this.transloco.translate('intake.public.error.parseFailed'));
        }
        this.loading.set(false);
      },
      error: (err) => {
        if (err.status === 404) {
          this.error.set(this.transloco.translate('intake.public.error.linkInvalid'));
        } else if (err.status === 410) {
          this.error.set(this.transloco.translate('intake.public.error.linkExpired'));
        } else {
          this.error.set(this.transloco.translate('intake.public.error.loadFailed'));
        }
        this.loading.set(false);
      }
    });
  }

}
