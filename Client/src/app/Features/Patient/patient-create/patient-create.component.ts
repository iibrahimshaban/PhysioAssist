import { Component, OnInit, ChangeDetectorRef, DestroyRef, inject, signal, computed } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SelectModule } from 'primeng/select';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, catchError, of, forkJoin, Subject } from 'rxjs';
import { PatientService } from '../services/patient.service';
import { DynamicFormEngineService } from '../../intake/services/dynamic-form-engine.service';
import { DynamicFormRendererComponent } from '../../intake/components/dynamic-form-renderer/dynamic-form-renderer.component';
import {
  DynamicFormSubmissionDto,
  FormSchemaResponse,
  FormSchemaSummaryResponse,
} from '../../intake/models';
import {
  BodyPainMapComponent,
  BodyPainMapPayload,
} from '../../intake/components/body-pain-map/body-pain-map.component';

@Component({
  selector: 'app-patient-create',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    SelectModule,
    DynamicFormRendererComponent,
    BodyPainMapComponent,
  ],
  templateUrl: './patient-create.component.html',
  styleUrl: './patient-create.component.css',
})
export class PatientCreateComponent implements OnInit {
  private readonly dynamicFormEngine = inject(DynamicFormEngineService);
  private readonly destroyRef = inject(DestroyRef);

  isLoadingSchemas = false;
  isLoadingForm = false;
  isSubmitting = false;
  errorMessage: string | null = null;

  availableSchemas: FormSchemaSummaryResponse[] = [];
  selectedSchemaId: string | null = null;

  formSchema: any = null;
  formSchemaVersion: number = 1;
  pendingSubmission: DynamicFormSubmissionDto | null = null;
  pendingPainMap: BodyPainMapPayload | null = null;

  readonly emailChecking = signal(false);
  readonly emailDuplicate = signal(false);
  readonly duplicateEmailAddress = signal<string | null>(null);

  readonly phoneChecking = signal(false);
  readonly phoneDuplicate = signal(false);
  readonly duplicatePhoneNumber = signal<string | null>(null);

  readonly canSubmit = computed(() =>
    !this.isSubmitting
    && !this.emailChecking()
    && !this.emailDuplicate()
    && !this.phoneChecking()
    && !this.phoneDuplicate()
  );

  private readonly submissionChanged$ = new Subject<DynamicFormSubmissionDto>();

  constructor(
    private patientService: PatientService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private location: Location
  ) {}

  ngOnInit() {
    this.isLoadingSchemas = true;
    this.patientService.getAllFormSchemas().subscribe({
      next: (schemas) => {
        this.availableSchemas = schemas;
        this.isLoadingSchemas = false;

        const defaultSchema = schemas.find((s) => s.isDefault) ?? schemas[0];
        if (defaultSchema) {
          this.selectedSchemaId = defaultSchema.id;
          this.loadSchema(defaultSchema.id);
        }

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = 'Failed to load intake forms for your account.';
        this.isLoadingSchemas = false;
        this.cdr.detectChanges();
      },
    });

    this.submissionChanged$.pipe(
      takeUntilDestroyed(this.destroyRef),
      debounceTime(600),
      distinctUntilChanged((a, b) =>
        this.extractEmail(a) === this.extractEmail(b) && this.extractPhone(a) === this.extractPhone(b)
      )
    ).subscribe(submission => {
      this.runEmailCheck(this.extractEmail(submission));
      this.runPhoneCheck(this.extractPhone(submission));
      this.cdr.detectChanges();
    });
  }

  private extractEmail(submission: DynamicFormSubmissionDto | null): string | null {
    if (!this.formSchema || !submission) return null;
    return this.dynamicFormEngine.extractEmailAnswer(this.formSchema, submission);
  }

  private extractPhone(submission: DynamicFormSubmissionDto | null): string | null {
    if (!this.formSchema || !submission) return null;
    return this.dynamicFormEngine.extractPhoneAnswer(this.formSchema, submission);
  }

  private runEmailCheck(email: string | null): void {
    if (!email) {
      this.emailDuplicate.set(false);
      this.duplicateEmailAddress.set(null);
      this.emailChecking.set(false);
      return;
    }
    this.emailChecking.set(true);
    this.patientService.checkPatientEmail(email).pipe(
      takeUntilDestroyed(this.destroyRef),
      catchError(() => of({ isRegistered: false }))
    ).subscribe(result => {
      this.emailChecking.set(false);
      this.emailDuplicate.set(result.isRegistered);
      this.duplicateEmailAddress.set(result.isRegistered ? email : null);
      this.cdr.detectChanges();
    });
  }

  private runPhoneCheck(phone: string | null): void {
    if (!phone) {
      this.phoneDuplicate.set(false);
      this.duplicatePhoneNumber.set(null);
      this.phoneChecking.set(false);
      return;
    }
    this.phoneChecking.set(true);
    this.patientService.checkPatientPhone(phone).pipe(
      takeUntilDestroyed(this.destroyRef),
      catchError(() => of({ isRegistered: false }))
    ).subscribe(result => {
      this.phoneChecking.set(false);
      this.phoneDuplicate.set(result.isRegistered);
      this.duplicatePhoneNumber.set(result.isRegistered ? phone : null);
      this.cdr.detectChanges();
    });
  }

  onSchemaSelected() {
    if (this.selectedSchemaId) {
      this.loadSchema(this.selectedSchemaId);
    }
  }

  private loadSchema(schemaId: string) {
    this.isLoadingForm = true;
    this.formSchema = null;
    this.pendingSubmission = null;
    this.resetDuplicateState();

    this.patientService.getFormSchema(schemaId).subscribe({
      next: (schemaResponse: FormSchemaResponse) => {
        this.formSchemaVersion = schemaResponse.version;
        try {
          this.formSchema = JSON.parse(schemaResponse.schemaJson);
        } catch {
          this.formSchema = null;
          this.errorMessage = 'Failed to parse the intake form schema.';
        }
        this.isLoadingForm = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = 'Failed to load the selected form.';
        this.isLoadingForm = false;
        this.cdr.detectChanges();
      },
    });
  }

  private resetDuplicateState(): void {
    this.emailDuplicate.set(false);
    this.duplicateEmailAddress.set(null);
    this.phoneDuplicate.set(false);
    this.duplicatePhoneNumber.set(null);
  }

  onSubmissionChange(submission: DynamicFormSubmissionDto) {
    this.pendingSubmission = submission;
    this.submissionChanged$.next(submission);
  }

  onPainMapChange(payload: BodyPainMapPayload) {
    this.pendingPainMap = payload;
  }

  submit() {
    if (!this.pendingSubmission || !this.selectedSchemaId) return;

    const email = this.extractEmail(this.pendingSubmission);
    const phone = this.extractPhone(this.pendingSubmission);

    if (email && this.emailDuplicate()) {
      this.errorMessage = `The email address ${email} is already associated with an existing patient record.`;
      return;
    }
    if (phone && this.phoneDuplicate()) {
      this.errorMessage = `The phone number ${phone} is already associated with an existing patient record.`;
      return;
    }

    this.verifyThenSubmit(email, phone);
  }

  private verifyThenSubmit(email: string | null, phone: string | null): void {
    const needsEmailCheck = !!email && !this.emailDuplicate() && !this.emailChecking();
    const needsPhoneCheck = !!phone && !this.phoneDuplicate() && !this.phoneChecking();

    if (!needsEmailCheck && !needsPhoneCheck) {
      this.doSubmit();
      return;
    }

    if (needsEmailCheck) this.emailChecking.set(true);
    if (needsPhoneCheck) this.phoneChecking.set(true);

    const email$ = needsEmailCheck
      ? this.patientService.checkPatientEmail(email!).pipe(catchError(() => of({ isRegistered: false })))
      : of(null);
    const phone$ = needsPhoneCheck
      ? this.patientService.checkPatientPhone(phone!).pipe(catchError(() => of({ isRegistered: false })))
      : of(null);

    forkJoin([email$, phone$]).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(([emailResult, phoneResult]) => {
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

      if (emailTaken || phoneTaken) {
        this.errorMessage = emailTaken && phoneTaken
          ? `Both the email address ${email} and the phone number ${phone} are already registered to an existing patient.`
          : emailTaken
            ? `The email address ${email} is already associated with an existing patient record.`
            : `The phone number ${phone} is already associated with an existing patient record.`;
        this.cdr.detectChanges();
        return;
      }

      this.doSubmit();
    });
  }

  private doSubmit(): void {
    this.isSubmitting = true;
    this.errorMessage = null;

    const painPointsData =
      this.pendingPainMap && this.pendingPainMap.regions.length > 0
        ? JSON.stringify(this.pendingPainMap)
        : undefined;

    this.patientService
      .createPatientFromIntake({
        formSchemaId: this.selectedSchemaId!,
        formSubmissionData: JSON.stringify(this.pendingSubmission),
        painPointsData,
      })
      .subscribe({
        next: (result) => {
          this.isSubmitting = false;
          this.router.navigate(['/app/initial-report', result.patientId]);
        },
        error: (err) => {
          console.error(err);
          this.errorMessage = err?.error?.detail || 'Failed to create the patient.';
          this.isSubmitting = false;
          this.cdr.detectChanges();
        },
      });
  }

  goBack() {
    this.location.back();
  }

  goForward() {
    this.location.forward();
  }
}