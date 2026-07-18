import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-submission-summary-stats',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './submission-summary-stats.component.html',
  styleUrl: './submission-summary-stats.component.css'
})
export class SubmissionSummaryStatsComponent {
  @Input({ required: true }) totalCount = 0;
  @Input({ required: true }) pendingCount = 0;
  @Input({ required: true }) approvedCount = 0;
  @Input({ required: true }) loading = false;
}
