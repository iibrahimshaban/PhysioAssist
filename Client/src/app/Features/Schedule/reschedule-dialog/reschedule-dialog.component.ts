// components/reschedule-dialog/reschedule-dialog.component.ts
import { Component, ChangeDetectionStrategy, input, output, inject, signal, effect, computed } from '@angular/core';
import { Appointment, AvailableInterval, DailyAvailability } from '../schedule.models';
import { SchedulePageService, toIsoWithOffset } from '../../../Core/Services/schedule-page.service';
import { DatePipe } from '@angular/common';


const LOOKAHEAD_DAYS = 30;

@Component({
  selector: 'app-reschedule-dialog',
  standalone: true,
  templateUrl: './reschedule-dialog.component.html',
  styleUrl: './reschedule-dialog.component.scss',
  imports:[DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RescheduleDialogComponent {
  isOpen = input<boolean>(false);
  appointment = input<Appointment | null>(null);

  closeRequested = output<void>();
  confirmRequested = output<{ appointmentId: string; newSlotStart: string; newSlotEnd: string }>();

  private readonly scheduleService = inject(SchedulePageService);

  protected readonly loading = signal(false);
  protected readonly workingDays = signal<DailyAvailability[]>([]);
  protected readonly selectedDay = signal<DailyAvailability | null>(null);
  protected readonly selectedInterval = signal<AvailableInterval | null>(null);
  protected readonly selectedTime = signal<string>(''); // "HH:mm" from the time input

  protected readonly originalDurationMinutes = computed(() => {
    const a = this.appointment();
    if (!a) return 0;
    return (a.slotEnd.getTime() - a.slotStart.getTime()) / 60000;
  });

  // Bounds for the native time input: can't start before the interval opens,
  // and can't start so late that (start + original duration) runs past the
  // interval's end — this is what lets the receptionist pick ANY valid minute,
  // not just the interval's start time.
  protected readonly timeInputMin = computed(() => {
    const interval = this.selectedInterval();
    console.log(interval)
    return interval ? this.toTimeString(interval.start) : '';
  });

  protected readonly timeInputMax = computed(() => {
    const interval = this.selectedInterval();
    const duration = this.originalDurationMinutes();
    if (!interval) return '';
    const latestStart = new Date(interval.end.getTime() - duration * 60000);
    return this.toTimeString(latestStart);
  });

  protected readonly isTimeValid = computed(() => {
    const time = this.selectedTime();
    const min = this.timeInputMin();
    const max = this.timeInputMax();
    if (!time || !min || !max) return false;
    return time >= min && time <= max;
  });

  constructor() {
    effect(() => {
      const a = this.appointment();
      if (a && this.isOpen()) {
        this.loadUpcomingWorkingDays(a.doctorId);
      }
    });
  }

  protected selectDay(day: DailyAvailability): void {
    this.selectedDay.set(day);
    this.selectedInterval.set(null);
    this.selectedTime.set('');
  }

  protected selectInterval(interval: AvailableInterval): void {
    this.selectedInterval.set(interval);
    this.selectedTime.set(this.toTimeString(interval.start)); // default to interval start, but fully editable
  }

  protected onTimeInput(event: Event): void {
    this.selectedTime.set((event.target as HTMLInputElement).value);
  }

  protected onConfirm(): void {
    const a = this.appointment();
    const day = this.selectedDay();
    const time = this.selectedTime();
    if (!a || !day || !this.isTimeValid()) return;

    const [h, m] = time.split(':').map(Number);
    const newStart = new Date(day.date);
    newStart.setHours(h, m, 0, 0);
    const newEnd = new Date(newStart.getTime() + this.originalDurationMinutes() * 60000);

    this.confirmRequested.emit({
      appointmentId: a.id,
      newSlotStart: toIsoWithOffset(newStart),
      newSlotEnd: toIsoWithOffset(newEnd)
    });
  }

  protected onClose(): void {
    this.selectedDay.set(null);
    this.selectedInterval.set(null);
    this.selectedTime.set('');
    this.closeRequested.emit();
  }

  private async loadUpcomingWorkingDays(doctorId: string): Promise<void> {
    this.loading.set(true);
    try {
      const from = new Date();
      const to = new Date();
      to.setDate(to.getDate() + LOOKAHEAD_DAYS);

      // Backend range cap is 31 days — LOOKAHEAD_DAYS stays safely under that.
      const days = await this.scheduleService.fetchAvailabilityRange(doctorId, from, to);

      // Only days with at least one free interval are useful to reschedule into.
      this.workingDays.set(days.filter(d => d.intervals.length > 0));
    } finally {
      this.loading.set(false);
    }
  }

  private toTimeString(date: Date): string {
    return `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
  }
}