import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SelectModule } from 'primeng/select';
import { PatientService } from '../services/patient.service';
import { DynamicFormRendererComponent } from '../../intake/components/dynamic-form-renderer/dynamic-form-renderer.component';
import {
  DynamicFormSubmissionDto,
  FormSchemaResponse,
  FormSchemaSummaryResponse,
} from '../../intake/models'; // adjust if BodyPainMapComponent's types aren't re-exported here too
import {
  BodyPainMapComponent,
  BodyPainMapPayload,
} from '../../intake/components/body-pain-map/body-pain-map.component'; // adjust path to actual location

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
  isLoadingSchemas = false;
  isLoadingForm = false;
  isSubmitting = false;
  errorMessage: string | null = null;

  availableSchemas: FormSchemaSummaryResponse[] = [];
  selectedSchemaId: string | null = null;

  formSchema: any = null; // parsed JSON — shape defined by the dynamic form builder, not a fixed DTO
  formSchemaVersion: number = 1;
  pendingSubmission: DynamicFormSubmissionDto | null = null;
  pendingPainMap: BodyPainMapPayload | null = null;

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

  onSubmissionChange(submission: DynamicFormSubmissionDto) {
    this.pendingSubmission = submission;
  }

  onPainMapChange(payload: BodyPainMapPayload) {
    this.pendingPainMap = payload;
  }

  submit() {
    if (!this.pendingSubmission || !this.selectedSchemaId) return;

    this.isSubmitting = true;
    this.errorMessage = null;

    // Only send pain points if the doctor actually marked at least one region —
    // an empty { regions: [] } isn't meaningfully different from "no pain data"
    // and shouldn't be persisted as such.
    const painPointsData =
      this.pendingPainMap && this.pendingPainMap.regions.length > 0
        ? JSON.stringify(this.pendingPainMap)
        : undefined;

    // One-shot: backend creates the patient first (duplicate-email checked there),
    // and only creates + wires the PreVisitIntake row if that succeeds.
    this.patientService
      .createPatientFromIntake({
        formSchemaId: this.selectedSchemaId,
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