import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PreVisitIntakeDetailsResponse, IntakeStatus } from '../../../models';

@Component({
  selector: 'app-submission-summary-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './submission-summary-card.component.html',
  styleUrl: './submission-summary-card.component.css'
})
export class SubmissionSummaryCardComponent {
  @Input({ required: true }) details!: PreVisitIntakeDetailsResponse;
  @Input() patientName?: string;
  @Input() patientEmail?: string;
  @Input() patientPhone?: string;

  getInitials(name: string | undefined): string {
    if (!name) return '?';
    return name.trim().split(/\s+/).map(n => n[0]).join('').toUpperCase().slice(0, 2);
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

  getStatusPillClass(status: IntakeStatus): string {
    switch (status) {
      case IntakeStatus.Pending:
      case IntakeStatus.Submitted:
      case IntakeStatus.InReview:
        return 'status-badge-warning';
      case IntakeStatus.Approved:
      case IntakeStatus.Converted:
        return 'status-badge-success';
      case IntakeStatus.Rejected:
      case IntakeStatus.Expired:
        return 'status-badge-danger';
      default:
        return 'status-badge-neutral';
    }
  }
}
