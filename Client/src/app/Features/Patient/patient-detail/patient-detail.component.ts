import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PatientService } from '../services/patient.service';

@Component({
  selector: 'app-patient-detail',
  imports: [CommonModule],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.css',
})
export class PatientDetailComponent implements OnInit {
  patient: any = null;
  isLoading = false;

  constructor(
    private patientService: PatientService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isLoading = true;
      this.patientService.getById(id).subscribe({
        next: (data) => {
          this.patient = data;
          this.isLoading = false;
        },
        error: (err) => {
          console.error(err);
          this.isLoading = false;
        }
      });
    }
  }

  goBack() {
    this.router.navigate(['/app/patients']);
  }

  goToEdit() {
    this.router.navigate(['/app/patients/edit', this.patient.id]);
  }

  delete() {
    if (confirm('Are you sure you want to delete this patient?')) {
      this.patientService.delete(this.patient.id).subscribe({
        next: () => this.goBack(),
        error: (err) => console.error(err)
      });
    }
  }
}