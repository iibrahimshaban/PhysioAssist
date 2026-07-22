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
  SubmitPreVisitIntakeRequest
} from '../../models';
import { BodyPainMapPayload, BodyPainMapComponent } from '../../components/body-pain-map/body-pain-map.component';

@Component({
  selector: 'app-public-intake',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
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

  readonly canSubmit = computed(() =>
    this.isFormValid()
    && !this.submitting()
    && !this.submitted()
  );

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

  painMapPayload = signal<BodyPainMapPayload | null>(null);

  onPainMapChange(payload: BodyPainMapPayload) {
    this.painMapPayload.set(payload);
  }

  submit(): void {
    const currentSubmission = this.submission();
    const currentSchema = this.schema();
    if (!currentSubmission || !currentSchema) return;

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