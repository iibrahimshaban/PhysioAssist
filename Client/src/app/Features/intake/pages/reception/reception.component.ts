import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IntakeApiService } from '../../services/intake-api.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import { PreVisitIntakeResponse, IntakeStatus } from '../../models';
import { SubmissionRowComponent } from '../submission-list/submission-row/submission-row.component';
import { SubmissionSummaryStatsComponent } from '../submission-list/submission-summary-stats/submission-summary-stats.component';
import { SubmissionFiltersBarComponent } from '../submission-list/submission-filters-bar/submission-filters-bar.component';

@Component({
  selector: 'app-reception',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    SubmissionRowComponent,
    SubmissionSummaryStatsComponent,
    SubmissionFiltersBarComponent
  ],
  templateUrl: './reception.component.html',
  styleUrl: './reception.component.css'
})
export class ReceptionComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly intakeApi = inject(IntakeApiService);
  private readonly snackbar = inject(SnackbarService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly submissions = signal<PreVisitIntakeResponse[]>([]);
  readonly searchTerm = signal('');
  readonly selectedStatus = signal<IntakeStatus | null>(null);
  readonly lastRefreshed = signal<Date | null>(null);

  // Pagination signals
  readonly currentPage = signal(1);
  readonly pageSize = signal(10);

  private autoRefreshHandle: ReturnType<typeof setInterval> | null = null;
  private loadRequestId = 0;

  readonly IntakeStatus = IntakeStatus;

  readonly statusCounts = computed(() => {
    const list = this.submissions();
    const map: Record<string | number, number> = {
      all: list.length,
      [IntakeStatus.Pending]: 0,
      [IntakeStatus.InReview]: 0
    };
    for (const s of list) {
      if (map[s.status] !== undefined) {
        map[s.status]++;
      }
    }
    return map;
  });

  readonly statusOptions = computed(() => {
    const counts = this.statusCounts();
    return [
      { label: 'All Waiting', value: null, count: counts['all'] },
      { label: 'Pending', value: IntakeStatus.Pending, count: counts[IntakeStatus.Pending] },
      { label: 'In Review', value: IntakeStatus.InReview, count: counts[IntakeStatus.InReview] }
    ];
  });

  readonly filteredSubmissions = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const status = this.selectedStatus();
    let list = this.submissions();

    if (status !== null) {
      list = list.filter(s => s.status === status);
    }

    if (!term) return list;
    return list.filter(s =>
      (s.patientName ?? '').toLowerCase().includes(term) ||
      (s.shortCode ?? '').toLowerCase().includes(term) ||
      `#${s.shortCode ?? ''}`.toLowerCase().includes(term)
    );
  });

  // Pagination computed values
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.filteredSubmissions().length / this.pageSize())));

  readonly paginatedSubmissions = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize();
    return this.filteredSubmissions().slice(start, start + this.pageSize());
  });

  readonly pageEndIndex = computed(() =>
    Math.min(this.currentPage() * this.pageSize(), this.filteredSubmissions().length)
  );

  readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const delta = 2;
    const range: number[] = [];

    for (let i = 1; i <= total; i++) {
      if (i === 1 || i === total || (i >= current - delta && i <= current + delta)) {
        range.push(i);
      }
    }
    return range;
  });

  readonly waitingCount = computed(() =>
    this.submissions().filter(s => s.status === IntakeStatus.Pending || s.status === IntakeStatus.Submitted).length
  );

  readonly inReviewCount = computed(() =>
    this.submissions().filter(s => s.status === IntakeStatus.InReview).length
  );

  readonly todayCount = computed(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return this.submissions().filter(s => new Date(s.submittedAt) >= today).length;
  });

  ngOnInit(): void {
    this.loadReception();
    this.startAutoRefresh();
  }

  startAutoRefresh(): void {
    this.autoRefreshHandle = setInterval(() => {
      this.loadReception(true);
    }, 30000);
    this.destroyRef.onDestroy(() => {
      if (this.autoRefreshHandle) clearInterval(this.autoRefreshHandle);
    });
  }

  loadReception(silent = false): void {
    const requestId = ++this.loadRequestId;
    if (!silent) {
      this.loading.set(true);
    }
    this.error.set(null);

    this.intakeApi.getSubmissions().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        if (requestId !== this.loadRequestId) return;
        this.submissions.set(data.filter(submission =>
          submission.status === IntakeStatus.Pending ||
          submission.status === IntakeStatus.Submitted ||
          submission.status === IntakeStatus.InReview
        ));
        this.loading.set(false);
        this.lastRefreshed.set(new Date());
      },
      error: () => {
        if (requestId !== this.loadRequestId) return;
        if (!silent) {
          this.error.set('Failed to load reception queue. Please try again.');
          this.snackbar.error('Error', ['Could not load pending intake submissions.']);
        }
        this.loading.set(false);
      }
    });
  }

  onSearch(term: string): void {
    this.searchTerm.set(term);
    this.currentPage.set(1);
  }

  onStatusChange(status: IntakeStatus | null): void {
    this.selectedStatus.set(status);
    this.currentPage.set(1);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedStatus.set(null);
    this.currentPage.set(1);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  viewSubmission(submission: PreVisitIntakeResponse): void {
    this.router.navigate(['/app/intake/submissions', submission.id]);
  }
}