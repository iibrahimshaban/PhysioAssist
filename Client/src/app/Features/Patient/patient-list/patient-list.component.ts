import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PatientService } from '../services/patient.service';
import { GenderPipe } from '...'; // update to actual path

@Component({
  selector: 'app-patient-list',
  imports: [CommonModule, FormsModule, GenderPipe],
  templateUrl: './patient-list.component.html',
  styleUrl: './patient-list.component.css',
})
export class PatientListComponent implements OnInit {
  patients = signal<any[]>([]);
  isLoading = signal(false);
  searchTerm = signal('');
  activeTab = signal<'all' | 'today' | 'pending'>('all');

  filteredPatients = computed(() => {
    let result = this.patients();

    const term = this.searchTerm().trim().toLowerCase();
    if (term) {
      result = result.filter(p =>
        p.fullName?.toLowerCase().includes(term) ||
        p.phoneNumber?.includes(term)
      );
    }

    const tab = this.activeTab();
    if (tab === 'today') {
      result = result.filter(p => p.slotStart && this.isToday(p.slotStart));
    } else if (tab === 'pending') {
      result = result.filter(p => !p.slotStart);
    }

    return result;
  });

  constructor(
    private patientService: PatientService,
    private router: Router,
  ) {}

  ngOnInit() {
    this.loadPatients();
  }

  loadPatients() {
    this.isLoading.set(true);
    this.patientService.getWithSlots().subscribe({
      next: (data) => {
        this.patients.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false);
      }
    });
  }

  private isToday(dateStr: string): boolean {
    const date = new Date(dateStr);
    const today = new Date();
    return date.getFullYear() === today.getFullYear()
        && date.getMonth() === today.getMonth()
        && date.getDate() === today.getDate();
  }

  setTab(tab: 'all' | 'today' | 'pending') {
    this.activeTab.set(tab);
  }

  getInitials(fullName: string): string {
    if (!fullName) return '?';
    return fullName.split(' ').map(n => n.charAt(0)).join('').substring(0, 2).toUpperCase();
  }

  goToDetail(id: string) {
    this.router.navigate(['app/patients', id]);
  }

  goToCreate() {
    this.router.navigate(['app/patients/create']);
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
}