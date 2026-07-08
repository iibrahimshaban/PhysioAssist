import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { ScheduleStatistics, Appointment, AvailableInterval, WorkingDayWindow, shortId } from '../schedule.models';

@Component({
  selector: 'app-statistics-panel',
  standalone: true,
  templateUrl: './statistics-panel.component.html',
  styleUrl: './statistics-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatisticsPanelComponent {
  statistics = input.required<ScheduleStatistics>();
  upcomingAppointment = input<Appointment | null>(null);
  nextAvailableSlot = input<AvailableInterval | null>(null);
  workingHours = input<WorkingDayWindow | null>(null);

  protected readonly shortId = shortId;

  protected formatTime(date: Date): string {
    return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
  }

  protected get workingHoursLabel(): string {
    const w = this.workingHours();
    if (!w) return 'Off today';
    return `${this.formatTimeString(w.startTime)} – ${this.formatTimeString(w.endTime)}`;
  }

  private formatTimeString(time: string): string {
    const [h, m] = time.split(':').map(Number);
    const period = h >= 12 ? 'PM' : 'AM';
    const displayHour = h > 12 ? h - 12 : h === 0 ? 12 : h;
    return `${displayHour}:${m.toString().padStart(2, '0')} ${period}`;
  }
}