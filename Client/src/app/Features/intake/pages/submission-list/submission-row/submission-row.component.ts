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

    // Ensure the string is parsed as UTC — backend sends DateTime.UtcNow,
    // but if the serialized string lacks a timezone suffix, JS parses it
    // as local time instead, throwing calculations off by the local UTC offset.
    const utcStr = /Z$|[+-]\d{2}:\d{2}$/.test(isoDate) ? isoDate : `${isoDate}Z`;
    const parsed = new Date(utcStr);
    if (isNaN(parsed.getTime())) return '';

    const diffMs = Date.now() - parsed.getTime();
    if (diffMs < 0) return 'just now';

    const minutes = Math.floor(diffMs / 60000);
    if (minutes < 1) return 'just now';
    if (minutes < 60) return `${minutes}m ago`;

    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;

    const days = Math.floor(hours / 24);
    if (days === 1) return 'Yesterday';
    if (days < 7) return `${days}d ago`;

    return parsed.toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      timeZone: 'Africa/Cairo'
    });
  }
}
