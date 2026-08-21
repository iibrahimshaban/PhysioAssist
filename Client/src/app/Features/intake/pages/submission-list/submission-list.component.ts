import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { IntakeApiService } from '../../services/intake-api.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import { PreVisitIntakeResponse, IntakeStatus, getIntakeStatusKey, getIntakeStatusPillClass, INTAKE_STATUS_KEYS } from '../../models';

import { SubmissionFiltersBarComponent } from './submission-filters-bar/submission-filters-bar.component';
import { SubmissionSummaryStatsComponent } from './submission-summary-stats/submission-summary-stats.component';
import { SubmissionRowComponent } from './submission-row/submission-row.component';
import { IntakePageContainerComponent } from '../../shared/intake-page-container.component';

@Component({
  selector: 'app-submission-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    TranslocoModule,
    SubmissionFiltersBarComponent,
    SubmissionSummaryStatsComponent,
    IntakePageContainerComponent,
    SubmissionRowComponent
  ],
  templateUrl: './submission-list.component.html',
  styleUrl: './submission-list.component.css'
})
export class SubmissionListComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly intakeApi = inject(IntakeApiService);
  private readonly snackbar = inject(SnackbarService);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly submissions = signal<PreVisitIntakeResponse[]>([]);
  readonly searchTerm = signal('');
  readonly selectedStatus = signal<IntakeStatus | null>(null);
  readonly sortOption = signal<string>('newest');
  readonly viewMode = signal<'cards' | 'table'>('cards');

  // Pagination signals
  readonly currentPage = signal(1);
  readonly pageSize = signal(10);

  private loadRequestId = 0;

  readonly statusCounts = computed(() => {
    const list = this.submissions();
    const map: Record<string | number, number> = {
      all: list.length,
      [IntakeStatus.Pending]: 0,
      [IntakeStatus.Submitted]: 0,
      [IntakeStatus.InReview]: 0,
      [IntakeStatus.Approved]: 0,
      [IntakeStatus.Rejected]: 0,
      [IntakeStatus.Converted]: 0,
      [IntakeStatus.Expired]: 0,
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
      { label: 'intake.filters.allStatuses', value: null, count: counts['all'] },
      { label: INTAKE_STATUS_KEYS[IntakeStatus.Pending], value: IntakeStatus.Pending, count: counts[IntakeStatus.Pending] },
      { label: INTAKE_STATUS_KEYS[IntakeStatus.Submitted], value: IntakeStatus.Submitted, count: counts[IntakeStatus.Submitted] },
      { label: INTAKE_STATUS_KEYS[IntakeStatus.InReview], value: IntakeStatus.InReview, count: counts[IntakeStatus.InReview] },
      { label: INTAKE_STATUS_KEYS[IntakeStatus.Approved], value: IntakeStatus.Approved, count: counts[IntakeStatus.Approved] },
      { label: INTAKE_STATUS_KEYS[IntakeStatus.Rejected], value: IntakeStatus.Rejected, count: counts[IntakeStatus.Rejected] },
      { label: INTAKE_STATUS_KEYS[IntakeStatus.Converted], value: IntakeStatus.Converted, count: counts[IntakeStatus.Converted] },
      { label: INTAKE_STATUS_KEYS[IntakeStatus.Expired], value: IntakeStatus.Expired, count: counts[IntakeStatus.Expired] },
    ];
  });

  readonly filteredSubmissions = computed(() => {
    let list = [...this.submissions()];

    // Filter by status
    const status = this.selectedStatus();
    if (status !== null) {
      list = list.filter(s => s.status === status);
    }

    // Filter by search term
    const term = this.searchTerm().toLowerCase().trim();
    if (term) {
      list = list.filter(s =>
        (s.patientName ?? '').toLowerCase().includes(term) ||
        (s.shortCode ?? '').toLowerCase().includes(term) ||
        `#${s.shortCode ?? ''}`.toLowerCase().includes(term)
      );
    }

    // Sort list
    const sort = this.sortOption();
    list.sort((a, b) => {
      if (sort === 'oldest') {
        return new Date(a.submittedAt).getTime() - new Date(b.submittedAt).getTime();
      }
      if (sort === 'name') {
        const nameA = (a.patientName || 'Unnamed').toLowerCase();
        const nameB = (b.patientName || 'Unnamed').toLowerCase();
        return nameA.localeCompare(nameB);
      }
      if (sort === 'pain') {
        return b.painRegionCount - a.painRegionCount;
      }
      // default 'newest'
      return new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime();
    });

    return list;
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
    const delta = 2; // pages to show around current
    const range: number[] = [];

    for (let i = 1; i <= total; i++) {
      if (i === 1 || i === total || (i >= current - delta && i <= current + delta)) {
        range.push(i);
      }
    }
    return range;
  });

  readonly pendingCount = computed(() =>
    this.submissions().filter(s => s.status === IntakeStatus.Pending || s.status === IntakeStatus.Submitted).length
  );

  readonly inReviewCount = computed(() =>
    this.submissions().filter(s => s.status === IntakeStatus.InReview).length
  );

  readonly approvedCount = computed(() =>
    this.submissions().filter(s => s.status === IntakeStatus.Approved).length
  );

  readonly convertedCount = computed(() =>
    this.submissions().filter(s => s.status === IntakeStatus.Converted).length
  );

  ngOnInit(): void {
    this.loadSubmissions();
  }

  loadSubmissions(): void {
    const requestId = ++this.loadRequestId;
    this.loading.set(true);
    this.error.set(null);
    this.currentPage.set(1);

    this.intakeApi.getSubmissions().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        if (requestId !== this.loadRequestId) return;
        this.submissions.set(data);
        this.loading.set(false);
      },
      error: () => {
        if (requestId !== this.loadRequestId) return;
        this.error.set(this.transloco.translate('intake.submissions.error.message'));
        this.loading.set(false);
        this.snackbar.error(this.transloco.translate('intake.snackbar.error'), [
          this.transloco.translate('intake.snackbar.couldNotLoadSubmissions')
        ]);
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

  onSortChange(sort: string): void {
    this.sortOption.set(sort);
    this.currentPage.set(1);
  }

  onViewModeChange(mode: 'cards' | 'table'): void {
    this.viewMode.set(mode);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedStatus.set(null);
    this.sortOption.set('newest');
    this.currentPage.set(1);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.currentPage.set(1);
  }

  viewSubmission(submission: PreVisitIntakeResponse): void {
    this.router.navigate(['/app/intake/submissions', submission.id]);
  }

  getInitials(name: string | undefined): string {
    if (!name || !name.trim()) return '?';
    const parts = name.trim().split(/\s+/);
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  getAvatarGradient(name: string | undefined, id: string): string {
    const gradients = [
      'linear-gradient(135deg, #1888CC 0%, #0FB3A5 100%)',
      'linear-gradient(135deg, #1a9dd4 0%, #12c4b4 100%)',
      'linear-gradient(135deg, #1078b0 0%, #0a8a7a 100%)',
      'linear-gradient(135deg, #5cb8e0 0%, #4FD4BC 100%)',
      'linear-gradient(135deg, #1888CC 0%, #087B6B 100%)',
      'linear-gradient(135deg, #90cbe8 0%, #1888CC 100%)',
    ];
    const key = name || id || 'default';
    let hash = 0;
    for (let i = 0; i < key.length; i++) {
      hash = key.charCodeAt(i) + ((hash << 5) - hash);
    }
    const index = Math.abs(hash) % gradients.length;
    return gradients[index];
  }

  getQueueStatusLabel(status: IntakeStatus): string {
    return this.transloco.translate(getIntakeStatusKey(status));
  }

  getStatusPillClass(status: IntakeStatus): string {
    return getIntakeStatusPillClass(status);
  }

  timeAgo(isoDate: string): string {
    if (!isoDate) return '';
    const diffMs = Date.now() - new Date(isoDate).getTime();
    const minutes = Math.floor(diffMs / 60000);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (minutes < 1) return this.transloco.translate('intake.common.time.justNow');
    if (minutes < 60) return this.transloco.translate('intake.common.time.mAgo', { n: minutes });
    if (hours < 24) return this.transloco.translate('intake.common.time.hAgo', { n: hours });
    return this.transloco.translate('intake.common.time.dAgo', { n: days });
  }
}