import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PatientService } from '../services/patient.service';
import { GenderPipe } from '../../../Shared/Pipes/gender-pipe';

@Component({
  selector: 'app-patient-list',
  imports: [CommonModule, GenderPipe],
  templateUrl: './patient-list.component.html',
  styleUrl: './patient-list.component.css',
})
export class PatientListComponent implements OnInit {
  patients: any[] = [];
  isLoading = false;

  constructor(
    private patientService: PatientService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadPatients();
  }

  loadPatients() {
    this.isLoading = true;
    this.patientService.getWithSlots().subscribe({
      next: (data) => {
        this.patients = data;
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

  getInitials(fullName: string): string {
    if (!fullName) return '?';
    return fullName.split(' ').map(n => n.charAt(0)).join('').substring(0, 2).toUpperCase();
  }

  goToDetail(id: string) {
    this.router.navigate(['/patients', id]);
  }

  goToCreate() {
    this.router.navigate(['/patients/create']);
  }
}