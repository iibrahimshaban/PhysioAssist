import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { Appointment, shortId } from '../schedule.models';

@Component({
  selector: 'app-appointment-drawer',
  standalone: true,
  templateUrl: './appointment-drawer.component.html',
  styleUrl: './appointment-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppointmentDrawerComponent {
  appointment = input<Appointment | null>(null);
  isOpen = input<boolean>(false);

  closeRequested = output<void>();
  completeRequested = output<string>();
  cancelRequested = output<string>();
  noShowRequested = output<string>();
  deleteRequested = output<string>();
  rescheduleRequested = output<Appointment>();

  protected readonly shortId = shortId;

  protected readonly durationLabel = computed(() => {
    const a = this.appointment();
    if (!a) return '';
    const minutes = (a.slotEnd.getTime() - a.slotStart.getTime()) / 60000;
    return minutes < 60 ? `${minutes} min` : `${(minutes / 60).toFixed(minutes % 60 === 0 ? 0 : 1)} hr`;
  });

  protected formatDateTime(date: Date): string {
    return date.toLocaleString('en-US', { weekday: 'short', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });
  }
}