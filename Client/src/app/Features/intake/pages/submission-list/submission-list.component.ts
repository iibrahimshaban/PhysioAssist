import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
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
    ButtonModule
  ],
  template: `
    <div class="max-w-3xl mx-auto py-6 px-4" aria-live="polite">

      <!-- Page Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h1 class="text-2xl font-extrabold text-slate-900 tracking-tight">Reception queue</h1>
          <p class="text-sm text-slate-500 mt-1">Patients who completed the pre-visit intake form</p>
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

      <!-- Error State -->
      @if (error()) {
        <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-8 text-center" role="alert">
          <div class="w-16 h-16 rounded-full bg-rose-50 text-rose-500 flex items-center justify-center mx-auto mb-4 text-2xl">
            <i class="pi pi-exclamation-triangle"></i>
          </div>
          <h3 class="text-base font-bold text-slate-900 mb-1">Failed to load submissions</h3>
          <p class="text-sm text-slate-500 mb-4">{{ error() }}</p>
          <p-button label="Try Again" icon="pi pi-refresh" severity="warn" (onClick)="loadSubmissions()" />
        </div>
      }

      @if (!error()) {
        <div class="bg-white rounded-2xl shadow-sm border border-slate-100 p-5 sm:p-6">

          <!-- Summary row -->
          @if (!loading() && submissions().length > 0) {
            <div class="flex flex-wrap gap-2 mb-5">
              <span class="text-[11px] font-semibold px-2.5 py-1 rounded-full bg-slate-100 text-slate-600">
                {{ submissions().length }} total
              </span>
              @if (pendingCount() > 0) {
                <span class="text-[11px] font-semibold px-2.5 py-1 rounded-full bg-amber-50 text-amber-700">
                  {{ pendingCount() }} pending review
                </span>
              }
              @if (approvedCount() > 0) {
                <span class="text-[11px] font-semibold px-2.5 py-1 rounded-full bg-emerald-50 text-emerald-700">
                  {{ approvedCount() }} approved
                </span>
              }
            </div>
          }

          <!-- Filters -->
          <div class="mb-5 flex flex-col sm:flex-row gap-3">
            <div class="relative flex-1">
              <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-sm"></i>
              <input
                type="search"
                [ngModel]="searchTerm()"
                (ngModelChange)="onSearch($event)"
                placeholder="Search by patient name..."
                class="w-full text-sm pl-9 pr-3 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-400"
                aria-label="Search submissions" />
            </div>
            <select
              [ngModel]="selectedStatus()"
              (ngModelChange)="onStatusChange($event)"
              class="w-full sm:w-52 text-sm px-3 py-2 border border-slate-200 rounded-xl bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-400"
              aria-label="Filter by status">
              @for (opt of statusOptions; track opt.label) {
                <option [ngValue]="opt.value">{{ opt.label }}</option>
              }
            </select>
          </div>

          <!-- Loading Skeleton -->
          @if (loading()) {
            <div class="space-y-3">
              @for (i of [1,2,3]; track i) {
                <div class="bg-slate-50 rounded-xl border border-slate-100 p-4 h-[72px] animate-pulse"></div>
              }
            </div>
          }

          <!-- Card list -->
          @if (!loading()) {
            @if (filteredSubmissions().length === 0) {
              <div class="text-center py-10">
                <div class="w-16 h-16 rounded-full bg-slate-50 text-slate-300 flex items-center justify-center mx-auto mb-4 text-2xl">
                  <i class="pi pi-inbox"></i>
                </div>
                <h3 class="text-base font-bold text-slate-900 mb-1">
                  @if (searchTerm() || selectedStatus()) { No matching submissions } @else { No submissions yet }
                </h3>
                <p class="text-sm text-slate-500 mb-4">
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
            } @else {
              <div class="space-y-3">
                @for (row of filteredSubmissions(); track row.id) {
                  <button
                    type="button"
                    (click)="viewSubmission(row)"
                    class="w-full text-left bg-slate-50 rounded-xl border border-slate-100 p-4 flex items-start sm:items-center justify-between gap-3 hover:border-indigo-200 hover:bg-white hover:shadow-sm transition">
                    <div class="flex items-center gap-3 min-w-0 flex-1">
                      <div class="w-8 h-8 sm:w-9 sm:h-9 rounded-full bg-indigo-50 text-indigo-600 flex items-center justify-center shrink-0 text-[11px] sm:text-xs font-bold">
                        {{ getInitials(row.patientName) }}
                      </div>
                      <div class="min-w-0 flex-1">
                        <div class="flex items-center gap-2 flex-wrap">
                          <span class="font-bold text-slate-900 capitalize text-sm sm:text-base truncate">{{ row.patientName || 'Unnamed patient' }}</span>
                          <span class="text-[11px] font-semibold px-2 py-0.5 rounded-full shrink-0"
                                [ngClass]="getStatusPillClass(row.status)">
                            {{ getQueueStatusLabel(row.status) }}
                          </span>
                        </div>
                        <p class="text-xs text-slate-400 mt-1 flex flex-wrap items-center gap-x-1.5 gap-y-0.5 m-0">
                          <span class="inline-flex items-center gap-1 whitespace-nowrap">
                            <i class="pi pi-clock text-[10px]"></i>
                            Checked in {{ timeAgo(row.submittedAt) }}
                          </span>
                          <span class="hidden sm:inline">·</span>
                          <span class="whitespace-nowrap">{{ row.painRegionCount }} pain region(s)</span>
                        </p>
                      </div>
                    </div>
                    <i class="pi pi-chevron-right text-slate-300 shrink-0 mt-1 sm:mt-0" aria-hidden="true"></i>
                  </button>
                }
              </div>
            }
          }
        </div>
      }
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
      (s.patientName ?? '').toLowerCase().includes(term)
    );
  });

  readonly pendingCount = computed(() =>
    this.submissions().filter(s =>
      s.status === IntakeStatus.Pending || s.status === IntakeStatus.Submitted || s.status === IntakeStatus.InReview
    ).length
  );

  readonly approvedCount = computed(() =>
    this.submissions().filter(s => s.status === IntakeStatus.Approved || s.status === IntakeStatus.Converted).length
  );

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

  onSearch(term: string): void {
    this.searchTerm.set(term);
    // Filtering itself is reactive via the filteredSubmissions computed signal.
  }

  onStatusChange(status: IntakeStatus | null): void {
    this.selectedStatus.set(status);
    this.loadSubmissions();
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedStatus.set(null);
    this.loadSubmissions();
  }

  viewSubmission(submission: PreVisitIntakeResponse): void {
    this.router.navigate(['app/intake/submissions', submission.id]);
  }

  getInitials(name: string | undefined): string {
    if (!name) return '?';
    return name.trim().split(/\s+/).map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  /**
   * Collapses Pending/Submitted/InReview into a single "Pending review" pill to
   * match the reception-queue design — this is a queue view, so anything not yet
   * finalized reads the same way. Adjust the status list here if you want a finer
   * split (e.g. show "In Review" separately).
   */
  getQueueStatusLabel(status: IntakeStatus): string {
    switch (status) {
      case IntakeStatus.Pending:
      case IntakeStatus.Submitted:
      case IntakeStatus.InReview:
        return 'Pending review';
      case IntakeStatus.Approved: return 'Approved';
      case IntakeStatus.Rejected: return 'Rejected';
      case IntakeStatus.Converted: return 'Converted';
      case IntakeStatus.Expired: return 'Expired';
      default: return 'Unknown';
    }
  }

  getStatusPillClass(status: IntakeStatus): string {
    switch (status) {
      case IntakeStatus.Pending:
      case IntakeStatus.Submitted:
      case IntakeStatus.InReview:
        return 'bg-amber-50 text-amber-700';
      case IntakeStatus.Approved:
      case IntakeStatus.Converted:
        return 'bg-emerald-50 text-emerald-700';
      case IntakeStatus.Rejected:
      case IntakeStatus.Expired:
        return 'bg-rose-50 text-rose-700';
      default:
        return 'bg-slate-100 text-slate-600';
    }
  }

  /** "Checked in Xd ago" style relative time, matching the reception-queue design. */
  timeAgo(isoDate: string): string {
    const diffMs = Date.now() - new Date(isoDate).getTime();
    const minutes = Math.floor(diffMs / 60000);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (minutes < 1) return 'just now';
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    return `${days}d ago`;
  }
}