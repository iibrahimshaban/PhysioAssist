import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PatientService } from '../services/patient.service';

@Component({
  selector: 'app-patient-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './patient-form.component.html',
  styleUrl: './patient-form.component.css',
})
export class PatientFormComponent implements OnInit {
  form: FormGroup;
  isEditMode = false;
  patientId: string | null = null;
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private patientService: PatientService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.form = this.fb.group({
      fullName: ['', Validators.required],
      dateOfBirth: ['', Validators.required],
      phoneNumber: ['', Validators.required],
      gender: ['', Validators.required],
      emailAddress: ['', Validators.email]
    });
  }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.patientId = id;
      this.patientService.getById(this.patientId).subscribe({
        next: (data) => this.form.patchValue(data),
        error: (err) => console.error(err)
      });
    }
  }

  submit() {
    if (this.form.invalid) return;
    this.isLoading = true;

    if (this.isEditMode && this.patientId) {
      this.patientService.update(this.patientId, this.form.value).subscribe({
        next: () => this.router.navigate(['app/patients']),
        error: (err) => {
          console.error(err);
          this.isLoading = false;
        }
      });
    } else {
      this.patientService.create(this.form.value).subscribe({
        next: () => this.router.navigate(['app/patients']),
        error: (err) => {
          console.error(err);
          this.isLoading = false;
        }
      });
    }
  }

  goBack() {

    this.router.navigate(['app/patients']);
  }
}