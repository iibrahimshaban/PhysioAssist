import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PreVisitIntakeResponse, IntakeStatus, getIntakeStatusLabel, getIntakeStatusPillClass } from '../../../models';

@Component({
  selector: 'app-submission-row',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './submission-row.component.html',
  styleUrl: './submission-row.component.css'
})
export class SubmissionRowComponent {
  @Input({ required: true }) submission!: PreVisitIntakeResponse;
  @Output() selected = new EventEmitter<PreVisitIntakeResponse>();

  readonly IntakeStatus = IntakeStatus;

  getInitials(name: string | undefined): string {
    if (!name || !name.trim()) return '?';
    const parts = name.trim().split(/\s+/);
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  getAvatarGradient(name: string | undefined, id: string): string {
    const gradients = [
      'linear-gradient(135deg, #0EB8A0 0%, #0B9A86 100%)', // Teal primary
      'linear-gradient(135deg, #4FD4BC 0%, #0EB8A0 100%)', // Teal light to primary
      'linear-gradient(135deg, #0B9A86 0%, #087B6B 100%)', // Teal dark
      'linear-gradient(135deg, #86E4D0 0%, #4FD4BC 100%)', // Teal lighter
      'linear-gradient(135deg, #0EB8A0 0%, #065E52 100%)', // Teal to very dark
      'linear-gradient(135deg, #B2EFE2 0%, #0EB8A0 100%)', // Teal lightest to primary
    ];
    const key = name || id || 'default';
    let hash = 0;
    for (let i = 0; i < key.length; i++) {
      hash = key.charCodeAt(i) + ((hash << 5) - hash);
    }
    const index = Math.abs(hash) % gradients.length;
    return gradients[index];
  }

  getDisplayName(submission: PreVisitIntakeResponse): string {
    if (submission.patientName && submission.patientName.trim()) {
      return submission.patientName.trim();
    }
    if (submission.shortCode) {
      return `Unnamed Patient (#${submission.shortCode})`;
    }
    return 'Unnamed Patient';
  }

  getShortCodeDisplay(code?: string): string {
    if (!code) return '';
    return code.startsWith('#') ? code : `#${code}`;
  }

  getQueueStatusLabel(status: IntakeStatus): string {
    return getIntakeStatusLabel(status);
  }

  getStatusPillClass(status: IntakeStatus): string {
    return getIntakeStatusPillClass(status);
  }

  getStatusAccentClass(status: IntakeStatus): string {
    switch (status) {
      case IntakeStatus.Pending:
      case IntakeStatus.Submitted:
        return 'accent-amber';
      case IntakeStatus.InReview:
        return 'accent-indigo';
      case IntakeStatus.Approved:
        return 'accent-emerald';
      case IntakeStatus.Rejected:
        return 'accent-red';
      case IntakeStatus.Converted:
        return 'accent-cyan';
      case IntakeStatus.Expired:
        return 'accent-gray';
      default:
        return 'accent-gray';
    }
  }

  getStatusDotClass(status: IntakeStatus): string {
    switch (status) {
      case IntakeStatus.Pending:
      case IntakeStatus.Submitted:
        return 'dot-amber';
      case IntakeStatus.InReview:
        return 'dot-indigo';
      case IntakeStatus.Approved:
        return 'dot-emerald';
      case IntakeStatus.Rejected:
        return 'dot-red';
      case IntakeStatus.Converted:
        return 'dot-cyan';
      case IntakeStatus.Expired:
        return 'dot-gray';
      default:
        return 'dot-gray';
    }
  }

  timeAgo(isoDate: string): string {
    if (!isoDate) return '';
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
