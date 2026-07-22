import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IntakeApiService } from '../../services/intake-api.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import { PreVisitIntakeResponse, IntakeStatus, getIntakeStatusLabel, getIntakeStatusPillClass } from '../../models';

import { SubmissionFiltersBarComponent } from './submission-filters-bar/submission-filters-bar.component';
import { SubmissionSummaryStatsComponent } from './submission-summary-stats/submission-summary-stats.component';
import { SubmissionRowComponent } from './submission-row/submission-row.component';

@Component({
  selector: 'app-submission-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    SubmissionFiltersBarComponent,
    SubmissionSummaryStatsComponent,
    SubmissionRowComponent
  ],
  templateUrl: './submission-list.component.html',
  styleUrl: './submission-list.component.css'
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
  private loadRequestId = 0;

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
    this.submissions().filter(s => s.status === IntakeStatus.Approved).length
  );

  readonly rejectedCount = computed(() =>
    this.submissions().filter(s => s.status === IntakeStatus.Rejected).length
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

    this.intakeApi.getSubmissions(this.selectedStatus() ?? undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        if (requestId !== this.loadRequestId) return;
        this.submissions.set(data);
        this.loading.set(false);
      },
      error: () => {
        if (requestId !== this.loadRequestId) return;
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
    this.router.navigate(['/app/intake/submissions', submission.id]);
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
    return getIntakeStatusLabel(status);
  }

  getStatusPillClass(status: IntakeStatus): string {
    return getIntakeStatusPillClass(status);
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
