
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AppointmentDrawerComponent } from './appointment-drawer/appointment-drawer.component';
import { CalendarGridComponent } from './calendar-grid/calendar-grid.component';
import { CalendarToolbarComponent } from './calendar-toolbar/calendar-toolbar.component';
import { CreateAppointmentDrawerComponent } from './create-appointment-drawer/create-appointment-drawer.component';
import { DoctorSelectorComponent } from './doctor-selector/doctor-selector.component';
import { EmptyStateComponent } from './empty-state/empty-state.component';
import { FiltersBarComponent } from './filters-bar/filters-bar.component';
import { LoadingSkeletonComponent } from './loading-skeleton/loading-skeleton.component';
import { Doctor, Appointment, AvailableInterval, CreateAppointmentRequest, ScheduleFilters, AvailableIntervalDto } from './schedule.models';
import { StatisticsPanelComponent } from './statistics-panel/statistics-panel.component';
import { SchedulePageService, toIsoWithOffset } from '../../Core/Services/schedule-page.service';
import { firstValueFrom } from 'rxjs';
import { RescheduleDialogComponent } from "./reschedule-dialog/reschedule-dialog.component";

@Component({
  selector: 'app-schedule-page',
  standalone: true,
  imports: [
    CalendarToolbarComponent, CalendarGridComponent, DoctorSelectorComponent,
    StatisticsPanelComponent, FiltersBarComponent, AppointmentDrawerComponent,
    CreateAppointmentDrawerComponent, EmptyStateComponent, LoadingSkeletonComponent,
    RescheduleDialogComponent
],
  templateUrl: './schedule-page.component.html',
  styleUrl: './schedule-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SchedulePageComponent {
  protected readonly scheduleService = inject(SchedulePageService);
  protected readonly isRescheduleDialogOpen = signal(false);
  protected readonly reschedulingAppointment = signal<Appointment | null>(null);

  // TEMPORARY placeholder — no real Doctor endpoint provided yet.
  protected readonly doctors: Doctor[] = [
    { id: '019f621c-c77d-72a7-8fdb-f31a445179bc', displayLabel: 'ahmed omar' },
    { id: '22222222-2222-2222-2222-222222222222', displayLabel: 'Doctor 2222-2222' },
    { id: '019e220e-ff37-7a97-9f65-0a8fa4861efb', displayLabel: 'Doctor Admin' }
  ];

  protected readonly dateRangeLabel = computed(() => {
    const date = this.scheduleService.selectedDate();
    const view = this.scheduleService.currentView();
    if (view === 'day') {
      return date.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' });
    }
    const start = new Date(date);
    start.setDate(start.getDate() - start.getDay());
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    return `${start.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} – ${end.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
  });

// BEFORE: this hid the whole calendar whenever there were zero appointments
// protected readonly emptyStateKind = computed(() => {
//   if (!this.scheduleService.selectedDoctorId()) return 'no-doctor' as const;
//   if (!this.scheduleService.workingHoursForSelectedDate()) return 'off-today' as const;
//   if (this.scheduleService.filteredAppointments().length === 0) return 'no-appointments' as const;
//   return null;
// });

protected readonly emptyStateKind = computed(() => {
  return !this.scheduleService.selectedDoctorId() ? ('no-doctor' as const) : null;
});

  protected onDoctorSelected(doctorId: string): void { this.scheduleService.selectDoctor(doctorId); }

  protected onPrevious(): void {
    this.shiftDate(this.scheduleService.currentView() === 'day' ? -1 : -7);
  }

  protected onNext(): void {
    this.shiftDate(this.scheduleService.currentView() === 'day' ? 1 : 7);
  }

  protected onToday(): void { this.scheduleService.goToToday(); }

  protected onAppointmentClicked(appointment: Appointment): void {
    this.scheduleService.openDetailsDrawer(appointment);
  }

  protected onIntervalClicked(interval: AvailableInterval): void {
    this.scheduleService.openCreateDrawer(interval.start, interval.end);
  }
//
  protected async onCreateSubmit(request: CreateAppointmentRequest): Promise<void> {
    console.log("////////////////////////")
    console.log(request)
    try {
      await this.scheduleService.createAppointment(request);
    } catch {
      // error toast already shown by the service
    }
  }

  protected async onReschedule(e: { appointment: Appointment; newStart: Date; newEnd: Date }): Promise<void> {
    this.scheduleService.optimisticallyMoveAppointment(e.appointment.id, e.newStart, e.newEnd);
    try {
      await this.scheduleService.rescheduleAppointment(e.appointment.id, {
        newSlotStart: toIsoWithOffset(e.newStart),
        newSlotEnd: toIsoWithOffset(e.newEnd)
      });
      this.scheduleService.showToast('Appointment rescheduled.', 'success');
    } catch {
      this.scheduleService.optimisticallyMoveAppointment(e.appointment.id, e.appointment.slotStart, e.appointment.slotEnd);
      this.scheduleService.showToast('Could not reschedule — restored original time.', 'error');
    }
  }

  protected async onQuickComplete(appointment: Appointment): Promise<void> {
    try { await this.scheduleService.completeAppointment(appointment.id); } catch { /* toast shown */ }
  }

  protected async onQuickCancel(appointment: Appointment): Promise<void> {
    try { await this.scheduleService.cancelAppointment(appointment.id); } catch { /* toast shown */ }
  }

  protected async onDrawerComplete(id: string): Promise<void> {
    try { await this.scheduleService.completeAppointment(id); } catch { /* toast shown */ }
  }

  protected async onDrawerCancel(id: string): Promise<void> {
    try { await this.scheduleService.cancelAppointment(id); } catch { /* toast shown */ }
  }

  protected async onDrawerNoShow(id: string): Promise<void> {
    try { await this.scheduleService.markNoShow(id); } catch { /* toast shown */ }
  }

  protected async onDrawerDelete(id: string): Promise<void> {
    try { await this.scheduleService.deleteAppointment(id); } catch { /* toast shown */ }
  }

  // protected onDrawerReschedule(appointment: Appointment): void {
  //   this.scheduleService.closeDetailsDrawer();
  //   this.scheduleService.openCreateDrawer(appointment.slotStart, appointment.slotEnd);
  //   // NOTE: this opens Create, not a true reschedule form. A dedicated
  //   // reschedule-in-place UI (reusing the same time-picker) is a reasonable
  //   // follow-up if this flow feels indirect in practice.
  // }



protected onDrawerReschedule(appointment: Appointment): void {
  this.scheduleService.closeDetailsDrawer();
  this.reschedulingAppointment.set(appointment);
  this.isRescheduleDialogOpen.set(true);
}

protected async onRescheduleConfirm(e: { appointmentId: string; newSlotStart: string; newSlotEnd: string }): Promise<void> {
  try {
    await this.scheduleService.rescheduleAppointment(e.appointmentId, {
      newSlotStart: e.newSlotStart,
      newSlotEnd: e.newSlotEnd
    });
    this.scheduleService.showToast('Appointment rescheduled.', 'success');
    this.isRescheduleDialogOpen.set(false);
  } catch {
    this.scheduleService.showToast('Could not reschedule to that time.', 'error');
  }
}

  protected onFiltersChanged(partial: Partial<ScheduleFilters>): void {
    this.scheduleService.updateFilters(partial);
  }

  private shiftDate(days: number): void {
    const current = this.scheduleService.selectedDate();
    const next = new Date(current);
    next.setDate(next.getDate() + days);
    this.scheduleService.selectDate(next);
  }


}

