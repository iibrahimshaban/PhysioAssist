import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { PreVisitIntakeDetailsResponse, IntakeStatus, getIntakeStatusKey } from '../../../models';

@Component({
  selector: 'app-submission-summary-card',
  standalone: true,
  imports: [CommonModule, TranslocoModule],
  templateUrl: './submission-summary-card.component.html',
  styleUrl: './submission-summary-card.component.css'
})
export class SubmissionSummaryCardComponent {
  @Input({ required: true }) details!: PreVisitIntakeDetailsResponse;
  @Input() patientName?: string;
  @Input() patientEmail?: string;
  @Input() patientPhone?: string;

  private readonly transloco = inject(TranslocoService);

  getInitials(name: string | undefined): string {
    if (!name) return '?';
    return name.trim().split(/\s+/).map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getStatusLabel(status: IntakeStatus): string {
    return this.transloco.translate(getIntakeStatusKey(status));
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
