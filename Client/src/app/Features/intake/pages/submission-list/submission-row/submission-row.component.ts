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

  getInitials(name: string | undefined): string {
    if (!name) return '?';
    return name.trim().split(/\s+/).map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getQueueStatusLabel(status: IntakeStatus): string {
    return getIntakeStatusLabel(status);
  }

  getStatusPillClass(status: IntakeStatus): string {
    return getIntakeStatusPillClass(status);
  }

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
