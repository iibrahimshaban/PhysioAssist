import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PatientService } from '../services/patient.service';

@Component({
  selector: 'app-patient-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './patient-list.component.html',
  styleUrl: './patient-list.component.css',
})
export class PatientListComponent implements OnInit {
  patients: any[] = [];
  isLoading = false;
  searchTerm = '';
  activeTab: 'all' | 'today' | 'pending' = 'all';

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

  get filteredPatients() {
    let result = this.patients;

    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter(p =>
        p.fullName?.toLowerCase().includes(term) ||
        p.phoneNumber?.includes(term)
      );
    }

    if (this.activeTab === 'today') {
      result = result.filter(p => p.slotStart && this.isToday(p.slotStart));
    } else if (this.activeTab === 'pending') {
      result = result.filter(p => !p.slotStart);
    }

    return result;
  }

  private isToday(dateStr: string): boolean {
    const date = new Date(dateStr);
    const today = new Date();
    return date.getFullYear() === today.getFullYear()
        && date.getMonth() === today.getMonth()
        && date.getDate() === today.getDate();
  }

  setTab(tab: 'all' | 'today' | 'pending') {
    this.activeTab = tab;
  }

  getInitials(fullName: string): string {
    if (!fullName) return '?';
    return fullName.split(' ').map(n => n.charAt(0)).join('').substring(0, 2).toUpperCase();
  }

  goToDetail(id: string) {
    this.router.navigate(['/app/patients', id]);
  }

  goToCreate() {
    this.router.navigate(['/app/patients/create']);
  }
}