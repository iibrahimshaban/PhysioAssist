import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PatientService } from '../services/patient.service';
import { GenderPipe } from '../../../Shared/Pipes/gender-pipe';


@Component({
  selector: 'app-patient-detail',
  imports: [CommonModule, GenderPipe],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.css',
})
export class PatientDetailComponent implements OnInit {
  patient: any = null;
  isLoading = false;

  constructor(
    private patientService: PatientService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isLoading = true;
      this.patientService.getById(id).subscribe({
        next: (data) => {
          this.patient = data;
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
    }
  }


  goBack() {
    this.router.navigate(['app/patients']);
  }

  goToEdit() {
    this.router.navigate(['app/patients/edit', this.patient.id]);
  }

  delete() {
    if (confirm('Are you sure you want to delete this patient?')) {
      this.patientService.delete(this.patient.id).subscribe({
        next: () => this.goBack(),
        error: (err) => console.error(err)
      });
    }
  }
  goToInitialReport(patient: any): void {
    this.router.navigate(['/app/initial-report', patient.id], {
      state: {
        patient: {
          id: patient.id,
          name: patient.fullName,
          gender: patient.gender,
        }
      }
    });
  }

  goToOverview() {
  this.router.navigate(['/app/patients', this.patient.id, 'overview']);
}
}