import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoModule } from '@jsverse/transloco';
import { IntakeStatus } from '../../../models';

@Component({
  selector: 'app-submission-summary-stats',
  standalone: true,
  imports: [CommonModule, TranslocoModule],
  templateUrl: './submission-summary-stats.component.html',
  styleUrl: './submission-summary-stats.component.css'
})
export class SubmissionSummaryStatsComponent {
  @Input({ required: true }) totalCount = 0;
  @Input({ required: true }) pendingCount = 0;
  @Input({ required: true }) inReviewCount = 0;
  @Input({ required: true }) approvedCount = 0;
  @Input({ required: true }) convertedCount = 0;
  @Input({ required: true }) loading = false;
  @Input() selectedStatus: IntakeStatus | null = null;
  @Input() mode: 'archive' | 'reception' = 'archive';
  @Input() todayCount = 0;

  @Output() selectStatus = new EventEmitter<IntakeStatus | null>();

  readonly IntakeStatus = IntakeStatus;

  onCardClick(status: IntakeStatus | null): void {
    this.selectStatus.emit(this.selectedStatus === status ? null : status);
  }
}
