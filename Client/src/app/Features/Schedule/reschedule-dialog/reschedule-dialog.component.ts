// components/reschedule-dialog/reschedule-dialog.component.ts
import { Component, ChangeDetectionStrategy, input, output, inject, signal, effect } from '@angular/core';
import { Appointment, AvailableInterval, WorkingDayWindow } from '../schedule.models';
import { SchedulePageService, toIsoWithOffset } from '../../../Core/Services/schedule-page.service';
import { DatePipe } from '@angular/common';


@Component({
  selector: 'app-reschedule-dialog',
  standalone: true,
  templateUrl: './reschedule-dialog.component.html',
  styleUrl: './reschedule-dialog.component.scss',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RescheduleDialogComponent {
  isOpen = input<boolean>(false);
  appointment = input<Appointment | null>(null);
  workingDays = input.required<WorkingDayWindow[]>();

  closeRequested = output<void>();
  confirmRequested = output<{ appointmentId: string; newSlotStart: string; newSlotEnd: string }>();

  private readonly scheduleService = inject(SchedulePageService);

  protected readonly selectedDate = signal<Date | null>(null);
  protected readonly availableIntervals = signal<AvailableInterval[]>([]);
  protected readonly selectedInterval = signal<AvailableInterval | null>(null);
  protected readonly loadingIntervals = signal(false);

  constructor() {
    // When the dialog opens for a NEW appointment, default the date picker
    // to that appointment's current date.
    effect(() => {
      const a = this.appointment();
      if (a && this.isOpen()) {
        this.selectedDate.set(new Date(a.slotStart));
      }
    });

    // Refetch available intervals every time the chosen date changes.
    effect(() => {
      const date = this.selectedDate();
      const a = this.appointment();
      if (!date || !a) return;
      this.loadIntervalsFor(date);
    });
  }

  protected isWorkingDay(date: Date): boolean {
    return this.workingDays().some(d => d.day === date.getDay());
  }

  protected onDateChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value; // "YYYY-MM-DD"
    const [y, m, d] = value.split('-').map(Number);
    const date = new Date(y, m - 1, d);

    if (!this.isWorkingDay(date)) return; // guard: never allow selecting a non-working day

    this.selectedDate.set(date);
    this.selectedInterval.set(null);
  }

  protected onSelectInterval(interval: AvailableInterval): void {
    this.selectedInterval.set(interval);
  }

  protected onConfirm(): void {
    const a = this.appointment();
    const interval = this.selectedInterval();
    if (!a || !interval) return;

    // Preserve the ORIGINAL duration when moving to the new time,
    // rather than assuming the whole available interval should be booked.
    const originalDurationMs = a.slotEnd.getTime() - a.slotStart.getTime();
    const newStart = interval.start;
    const newEnd = new Date(newStart.getTime() + originalDurationMs);

    this.confirmRequested.emit({
      appointmentId: a.id,
      newSlotStart: toIsoWithOffset(newStart),
      newSlotEnd: toIsoWithOffset(newEnd)
    });
  }

  protected onClose(): void {
    this.selectedInterval.set(null);
    this.closeRequested.emit();
  }

  private async loadIntervalsFor(date: Date): Promise<void> {
    const a = this.appointment();
    if (!a) return;
    this.loadingIntervals.set(true);
    try {
      this.availableIntervals.set(await this.scheduleService.fetchAvailabilityForDate(a.doctorId, date));
    } finally {
      this.loadingIntervals.set(false);
    }
  }
}