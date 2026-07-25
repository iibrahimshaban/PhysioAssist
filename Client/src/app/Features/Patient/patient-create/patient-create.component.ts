import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SelectModule } from 'primeng/select';
import { PatientService } from '../services/patient.service';
import { DynamicFormRendererComponent } from '../../intake/components/dynamic-form-renderer/dynamic-form-renderer.component';
import { DynamicFormSubmissionDto } from '../../intake/models';

@Component({
  selector: 'app-patient-create',
  standalone: true,
  imports: [CommonModule, FormsModule, SelectModule, DynamicFormRendererComponent],
  templateUrl: './patient-create.component.html',
  styleUrl: './patient-create.component.css',
})
export class PatientCreateComponent implements OnInit {
  isLoadingSchemas = false;
  isLoadingForm = false;
  isSubmitting = false;
  errorMessage: string | null = null;

  availableSchemas: any[] = [];
  selectedSchemaId: string | null = null;

  formSchema: any = null;
  formSchemaVersion: number = 1;
  pendingSubmission: DynamicFormSubmissionDto | null = null;

  constructor(
    private patientService: PatientService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.isLoadingSchemas = true;
    this.patientService.getAllFormSchemas().subscribe({
      next: (schemas) => {
        this.availableSchemas = schemas;
        this.isLoadingSchemas = false;

        const defaultSchema = schemas.find((s: any) => s.isDefault) ?? schemas[0];
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
      }
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
      next: (schemaResponse) => {
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
      }
    });
  }

  onSubmissionChange(submission: DynamicFormSubmissionDto) {
    this.pendingSubmission = submission;
  }

  submit() {
    if (!this.pendingSubmission || !this.selectedSchemaId) return;

    this.isSubmitting = true;
    this.errorMessage = null;

    const body = {
      formSchemaId: this.selectedSchemaId,
      formSubmissionData: JSON.stringify(this.pendingSubmission),
      painPointsData: null
    };

    this.patientService.createPatientFromIntake(body).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        this.router.navigate(['/app/patients', response.patientId, 'overview']);
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = err?.error?.detail || 'Failed to create the patient.';
        this.isSubmitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  goBack() {
    this.router.navigate(['/app/patients']);
  }
}