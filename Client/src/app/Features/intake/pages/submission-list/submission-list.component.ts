import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IntakeApiService } from '../../services/intake-api.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import { PreVisitIntakeResponse, IntakeStatus } from '../../models';

@Component({
  selector: 'app-submission-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    TagModule,
    CardModule,
    MessageModule
  ],
  template: `
    <div class="page-container animate-fade-in" aria-live="polite">

      <!-- Page Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-xl flex items-center justify-center"
               style="background: linear-gradient(135deg, #8b5cf6 0%, #ec4899 100%);">
            <i class="pi pi-inbox text-white text-lg"></i>
          </div>
          <div>
            <h1 class="page-title" id="submissions-heading">Intake Submissions</h1>
            <p class="page-subtitle">
              Review and manage patient intake forms
              @if (!loading() && !error()) {
                <span class="ml-1.5 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-surface-100 text-surface-600">
                  {{ filteredSubmissions().length }} total
                </span>
              }
            </p>
          </div>
        </div>
        <p-button
          label="Refresh"
          icon="pi pi-refresh"
          [text]="true"
          severity="secondary"
          (onClick)="loadSubmissions()"
          [loading]="loading()"
          aria-label="Refresh submissions list">
        </p-button>
      </div>

      <p-card>
        <ng-template pTemplate="content">

          <!-- Error State -->
          @if (error()) {
            <div class="empty-state py-12 animate-fade-in-up" role="alert">
              <div class="empty-state-icon" style="background: #fef2f2; color: #ef4444; width: 5rem; height: 5rem; font-size: 2rem;">
                <i class="pi pi-exclamation-triangle"></i>
              </div>
              <h3 class="empty-state-title">Failed to load submissions</h3>
              <p class="empty-state-text">{{ error() }}</p>
              <p-button label="Try Again" icon="pi pi-refresh" severity="warn" (onClick)="loadSubmissions()" />
            </div>
          }

          @if (!error()) {
            <!-- Filters Bar -->
            <div class="mb-5 flex flex-col sm:flex-row gap-3" role="search" aria-label="Filter submissions">
              <div class="relative flex-1">
                <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
                <input
                  pInputText
                  type="search"
                  [(ngModel)]="searchTerm"
                  (input)="onSearch()"
                  placeholder="Search by patient name or email..."
                  class="w-full !pl-10"
                  style="padding-left: 2.5rem !important;"
                  aria-label="Search submissions" />
              </div>
              <p-select
                [options]="statusOptions"
                [(ngModel)]="selectedStatus"
                (ngModelChange)="onStatusChange()"
                placeholder="All statuses"
                class="w-full sm:w-52"
                aria-label="Filter by status">
              </p-select>
            </div>

            <!-- Loading Skeleton -->
            @if (loading()) {
              <div class="stagger-children">
                @for (i of [1,2,3,4,5]; track i) {
                  <div class="skeleton-row">
                    <div class="flex items-center gap-3" style="width: 200px;">
                      <div class="skeleton skeleton-circle" style="width: 32px; height: 32px;"></div>
                      <div class="skeleton skeleton-text" style="width: 120px;"></div>
                    </div>
                    <div class="skeleton skeleton-text" style="width: 160px;"></div>
                    <div class="skeleton skeleton-text" style="width: 100px;"></div>
                    <div class="skeleton" style="width: 80px; height: 24px; border-radius: 9999px;"></div>
                    <div class="skeleton skeleton-text" style="width: 90px;"></div>
                  </div>
                }
              </div>
            }

            <!-- Table -->
            @if (!loading()) {
              <p-table
                [value]="filteredSubmissions()"
                [paginator]="true"
                [rows]="10"
                [showCurrentPageReport]="true"
                currentPageReportTemplate="Showing {first} to {last} of {totalRecords} submissions"
                [rowHover]="true"
                styleClass="p-datatable-sm cursor-pointer"
                [tableStyle]="{ 'min-width': '40rem' }">

                <ng-template pTemplate="header">
                  <tr>
                    <th scope="col">Patient</th>
                    <th scope="col" class="hide-on-mobile">Email</th>
                    <th scope="col" class="hide-on-mobile">Phone</th>
                    <th scope="col">Status</th>
                    <th scope="col">Submitted</th>
                  </tr>
                </ng-template>

                <ng-template pTemplate="body" let-row>
                  <tr class="cursor-pointer animate-fade-in" (click)="viewSubmission(row)">
                    <td>
                      <div class="flex items-center gap-2.5">
                        <!-- Patient avatar -->
                        <div class="w-8 h-8 rounded-full flex items-center justify-center shrink-0 text-xs font-bold text-white"
                             [style.background]="getAvatarGradient(row.patientName)">
                          {{ getInitials(row.patientName) }}
                        </div>
                        <span class="font-semibold text-surface-800">{{ row.patientName }}</span>
                      </div>
                    </td>
                    <td class="hide-on-mobile">
                      <span class="text-surface-500 text-sm">{{ row.patientEmail || '—' }}</span>
                    </td>
                    <td class="hide-on-mobile">
                      <span class="text-surface-500 text-sm">{{ row.patientPhone || '—' }}</span>
                    </td>
                    <td>
                      <div class="flex items-center gap-1.5">
                        <span class="status-dot" [class]="'status-dot-' + getStatusDotColor(row.status)"></span>
                        <p-tag
                          [value]="getStatusLabel(row.status)"
                          [severity]="getStatusSeverity(row.status)" />
                      </div>
                    </td>
                    <td>
                      <span class="text-sm text-surface-500">{{ row.submittedAt | date:'MMM d, y' }}</span>
                    </td>
                  </tr>
                </ng-template>

                <ng-template pTemplate="emptymessage">
                  <tr>
                    <td colspan="5">
                      <div class="empty-state py-12">
                        <div class="empty-state-icon" style="width: 5rem; height: 5rem; font-size: 2rem;">
                          <i class="pi pi-inbox"></i>
                        </div>
                        <h3 class="empty-state-title">
                          @if (searchTerm() || selectedStatus()) {
                            No matching submissions
                          } @else {
                            No submissions yet
                          }
                        </h3>
                        <p class="empty-state-text">
                          @if (searchTerm() || selectedStatus()) {
                            Try adjusting your search or filter criteria.
                          } @else {
                            Patient submissions will appear here once forms are completed.
                          }
                        </p>
                        @if (searchTerm() || selectedStatus()) {
                          <p-button label="Clear Filters" icon="pi pi-filter-slash" [text]="true" (onClick)="clearFilters()" />
                        }
                      </div>
                    </td>
                  </tr>
                </ng-template>
              </p-table>
            }
          }

        </ng-template>
      </p-card>
    </div>
  `,
  styles: [`
    :host { display: block; }
  `]
})
export class SubmissionListComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly intakeApi = inject(IntakeApiService);
  private readonly snackbar = inject(SnackbarService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly submissions = signal<PreVisitIntakeResponse[]>([]);
  readonly searchTerm = signal('');
  readonly selectedStatus = signal<IntakeStatus | null>(null);

  readonly statusOptions = [
    { label: 'All Statuses', value: null },
    { label: 'Pending', value: IntakeStatus.Pending },
    { label: 'Submitted', value: IntakeStatus.Submitted },
    { label: 'In Review', value: IntakeStatus.InReview },
    { label: 'Approved', value: IntakeStatus.Approved },
    { label: 'Rejected', value: IntakeStatus.Rejected },
    { label: 'Converted', value: IntakeStatus.Converted },
    { label: 'Expired', value: IntakeStatus.Expired },
  ];

  readonly filteredSubmissions = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.submissions();
    return this.submissions().filter(s =>
      s.patientName.toLowerCase().includes(term) ||
      (s.patientEmail?.toLowerCase().includes(term) ?? false)
    );
  });

  ngOnInit(): void {
    this.loadSubmissions();
  }

  loadSubmissions(): void {
    this.loading.set(true);
    this.error.set(null);

    this.intakeApi.getSubmissions(this.selectedStatus() ?? undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.submissions.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load submissions. Please try again.');
        this.loading.set(false);
        this.snackbar.error('Error', ['Could not load intake submissions.']);
      }
    });
  }

  onSearch(): void {
    // Filtering is reactive via computed signal
  }

  onStatusChange(): void {
    this.loadSubmissions();
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedStatus.set(null);
    this.loadSubmissions();
  }

  viewSubmission(submission: PreVisitIntakeResponse): void {
    this.router.navigate(['/intake/submissions', submission.id]);
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getAvatarGradient(name: string): string {
    const gradients = [
      'linear-gradient(135deg, #6366f1, #8b5cf6)',
      'linear-gradient(135deg, #ec4899, #f43f5e)',
      'linear-gradient(135deg, #14b8a6, #06b6d4)',
      'linear-gradient(135deg, #f59e0b, #ef4444)',
      'linear-gradient(135deg, #22c55e, #16a34a)',
      'linear-gradient(135deg, #3b82f6, #6366f1)',
    ];
    const index = name.charCodeAt(0) % gradients.length;
    return gradients[index];
  }

  getStatusDotColor(status: IntakeStatus): string {
    switch (status) {
      case IntakeStatus.Approved: return 'success';
      case IntakeStatus.Converted: return 'success';
      case IntakeStatus.InReview: return 'warning';
      case IntakeStatus.Rejected: return 'danger';
      case IntakeStatus.Expired: return 'danger';
      default: return 'info';
    }
  }

  getStatusLabel(status: IntakeStatus): string {
    switch (status) {
      case IntakeStatus.Pending: return 'Pending';
      case IntakeStatus.Submitted: return 'Submitted';
      case IntakeStatus.InReview: return 'In Review';
      case IntakeStatus.Approved: return 'Approved';
      case IntakeStatus.Rejected: return 'Rejected';
      case IntakeStatus.Converted: return 'Converted';
      case IntakeStatus.Expired: return 'Expired';
      default: return 'Unknown';
    }
  }

  getStatusSeverity(status: IntakeStatus): 'info' | 'warn' | 'success' | 'danger' | 'secondary' | 'contrast' {
    switch (status) {
      case IntakeStatus.Pending: return 'info';
      case IntakeStatus.Submitted: return 'info';
      case IntakeStatus.InReview: return 'warn';
      case IntakeStatus.Approved: return 'success';
      case IntakeStatus.Rejected: return 'secondary';
      case IntakeStatus.Converted: return 'success';
      case IntakeStatus.Expired: return 'secondary';
      default: return 'info';
    }
  }
}
