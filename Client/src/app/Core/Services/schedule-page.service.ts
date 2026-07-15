import { Injectable, signal, computed, effect, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError, firstValueFrom, Observable, MonoTypeOperatorFunction } from 'rxjs';
import {
  ScheduleSlotDto, AvailableIntervalDto, CreateAppointmentRequest, RescheduleAppointmentRequest,
  WorkingScheduleDto, ProblemDetails, CalendarViewMode, ScheduleFilters, ScheduleStatistics,
  Appointment, AvailableInterval, WorkingDayWindow, ToastMessage,
  DailyAvailabilityDto,
  DailyAvailability
} from '../../Features/Schedule/schedule.models';
import { environment } from '../../../environments/environment.development';

const APPOINTMENTS_BASE = `${environment.apiUrl}appointments`;
const WORKING_SCHEDULES_BASE = `${environment.apiUrl}workingschedules`;

@Injectable({ providedIn: 'root' })
export class SchedulePageService {
  private readonly http = inject(HttpClient);
  private toastCounter = 0;

  readonly selectedDoctorId = signal<string | null>(null);
  readonly selectedDate = signal<Date>(new Date());
  readonly currentView = signal<CalendarViewMode>('week');

  readonly appointments = signal<Appointment[]>([]);
  readonly availability = signal<AvailableInterval[]>([]);
  readonly workingDays = signal<WorkingDayWindow[]>([]);

  readonly selectedAppointment = signal<Appointment | null>(null);
  readonly isDetailsDrawerOpen = signal(false);
  readonly isCreateDrawerOpen = signal(false);
  readonly createDrawerPrefill = signal<{ start: Date; end: Date } | null>(null);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly toasts = signal<ToastMessage[]>([]);

  readonly filters = signal<ScheduleFilters>({
    patientSearch: '',
    statuses: new Set(),
    showOnlyToday: false
  });

  readonly filteredAppointments = computed(() => {
    const list = this.appointments();
    const f = this.filters();
    const today = new Date();

    return list.filter(a => {
      if (f.statuses.size > 0 && !f.statuses.has(a.status)) return false;
      if (f.showOnlyToday && a.slotStart.toDateString() !== today.toDateString()) return false;
      if (f.patientSearch.trim().length > 0) {
        return a.patientId.toLowerCase().includes(f.patientSearch.trim().toLowerCase());
      }
      return true;
    });
  });

  readonly workingHoursForSelectedDate = computed<WorkingDayWindow | null>(() => {
    const dow = this.selectedDate().getDay();
    return this.workingDays().find(d => d.day === dow) ?? null;
  });

  readonly statistics = computed<ScheduleStatistics>(() => {
    const today = new Date();
    const todays = this.appointments().filter(a => a.slotStart.toDateString() === today.toDateString());
    const completed = todays.filter(a => a.status === 'Completed').length;
    const cancelled = todays.filter(a => a.status === 'Cancelled').length;
    const noShow = todays.filter(a => a.status === 'NoShow').length;
    const totalDuration = todays.reduce((s, a) => s + (a.slotEnd.getTime() - a.slotStart.getTime()) / 60000, 0);
    const freeMinutes = this.availability().reduce((s, i) => s + (i.end.getTime() - i.start.getTime()) / 60000, 0);
    const window = this.workingHoursForSelectedDate();
    const workingMinutes = window ? this.windowDurationMinutes(window) : 0;

    return {
      appointmentsToday: todays.length,
      completedToday: completed,
      cancelledToday: cancelled,
      noShowToday: noShow,
      freeMinutesRemaining: Math.round(freeMinutes),
      occupancyPercent: workingMinutes > 0 ? Math.round(((workingMinutes - freeMinutes) / workingMinutes) * 100) : 0,
      averageDurationMinutes: todays.length > 0 ? Math.round(totalDuration / todays.length) : 0
    };
  });

  readonly upcomingAppointment = computed<Appointment | null>(() => {
    const now = new Date();
    return this.appointments()
      .filter(a => a.status === 'Booked' && a.slotStart > now)
      .sort((a, b) => a.slotStart.getTime() - b.slotStart.getTime())[0] ?? null;
  });

  readonly nextAvailableSlot = computed<AvailableInterval | null>(() => {
    const now = new Date();
    return this.availability()
      .filter(i => i.end > now)
      .sort((a, b) => a.start.getTime() - b.start.getTime())[0] ?? null;
  });

  constructor() {
    effect(() => {
      const doctorId = this.selectedDoctorId();
      const date = this.selectedDate();
      const view = this.currentView();
      if (!doctorId) return;
      this.loadForCurrentSelection(doctorId, date, view);
    });

    effect(() => {
      const doctorId = this.selectedDoctorId();
      if (!doctorId) return;
      this.loadWorkingSchedule(doctorId);
    });
  }

  selectDoctor(doctorId: string): void { this.selectedDoctorId.set(doctorId); }
  selectDate(date: Date): void { this.selectedDate.set(date); }
  goToToday(): void { this.selectedDate.set(new Date()); }
  setView(view: CalendarViewMode): void { this.currentView.set(view); }

  openDetailsDrawer(appointment: Appointment): void {
    this.selectedAppointment.set(appointment);
    this.isDetailsDrawerOpen.set(true);
  }

  closeDetailsDrawer(): void {
    this.isDetailsDrawerOpen.set(false);
    this.selectedAppointment.set(null);
  }

  openCreateDrawer(start: Date, end: Date): void {
    this.createDrawerPrefill.set({ start, end });
    this.isCreateDrawerOpen.set(true);
  }

  closeCreateDrawer(): void {
    this.isCreateDrawerOpen.set(false);
    this.createDrawerPrefill.set(null);
  }

  updateFilters(partial: Partial<ScheduleFilters>): void {
    this.filters.update(c => ({ ...c, ...partial }));
  }

  clearFilters(): void {
    this.filters.set({ patientSearch: '', statuses: new Set(), showOnlyToday: false });
  }

  showToast(text: string, kind: 'success' | 'error'): void {
    const id = ++this.toastCounter;
    this.toasts.update(list => [...list, { id, text, kind }]);
    setTimeout(() => this.toasts.update(list => list.filter(t => t.id !== id)), 4000);
  }

//   // schedule-page.service.ts — add: pure fetch, does not touch the main availability signal
// async fetchAvailabilityForDate(doctorId: string, date: Date): Promise<AvailableInterval[]> {
//   const dtos = await firstValueFrom(
//     this.http.get<AvailableIntervalDto[]>(`${APPOINTMENTS_BASE}/doctor/${doctorId}/availability`, {
//       params: { date: date.toISOString() }
//     }).pipe(this.catchAsProblem())
//   );
//   return dtos.map(d => ({ start: new Date(d.start), end: new Date(d.end) }));
// }


  // schedule-page.service.ts

// // New: single round trip for a date range, replaces the old N-calls-per-week
// // workaround. Backend omits non-working days entirely from the response,
// // so anything NOT in this list is implicitly a day the doctor doesn't work.
// async fetchAvailabilityRange(doctorId: string, from: Date, to: Date): Promise<DailyAvailability[]> {
//   const dtos = await firstValueFrom(
//     this.http.get<DailyAvailabilityDto[]>(`${APPOINTMENTS_BASE}/doctor/${doctorId}/availability-range`, {
//       params: { from: toIsoWithOffset(from), to: toIsoWithOffset(to) }
//     }).pipe(this.catchAsProblem())
//   );

//   return dtos.map(d => ({
//     date: this.parseDateOnly(d.date),
//     intervals: d.intervals.map(i => ({ start: new Date(i.start), end: new Date(i.end) }))
//   }));
// }

// private parseDateOnly(value: string): Date {
//   const [y, m, d] = value.split('-').map(Number);
//   return new Date(y, m - 1, d);
// }



async fetchAvailabilityRange(doctorId: string, from: Date, to: Date): Promise<DailyAvailability[]> {
  const dtos = await firstValueFrom(
    this.http.get<DailyAvailabilityDto[]>(`${APPOINTMENTS_BASE}/doctor/${doctorId}/availability-range`, {
      params: { from: toIsoWithOffset(from), to: toIsoWithOffset(to) }
    }).pipe(this.catchAsProblem())
  );

  // Backend now sends intervals as time-only strings ("09:00:00") scoped to
  // the day's own "date" field, instead of full ISO datetimes. Each interval
  // must be reconstructed by combining the parent day's date with its time.
  return dtos.map(d => {
    const date = this.parseDateOnly(d.date);
    return {
      date,
      intervals: d.intervals.map(i => ({
        start: this.combineDateAndTime(date, i.start),
        end: this.combineDateAndTime(date, i.end)
      }))
    };
  });
}

private parseDateOnly(value: string): Date {
  const [y, m, d] = value.split('-').map(Number);
  return new Date(y, m - 1, d);
}

// Combines a calendar date (midnight, local time) with a "HH:mm:ss" time
// string into a single Date. Assumes an interval never crosses midnight —
// matches working-hours semantics (an end time of "00:00:00" would need
// special-casing as next-day, which isn't a real working-hours scenario).
private combineDateAndTime(date: Date, time: string): Date {
  const [h, m, s] = time.split(':').map(Number);
  const result = new Date(date);
  result.setHours(h, m, s || 0, 0);
  return result;
}



  async createAppointment(request: CreateAppointmentRequest): Promise<void> {
    try {
      const dto = await firstValueFrom(
        this.http.post<ScheduleSlotDto>(APPOINTMENTS_BASE, request).pipe(this.catchAsProblem())
      );
      this.appointments.update(list => [...list, this.toAppointment(dto)]);
      await this.refreshAvailability();
      this.closeCreateDrawer();
      this.showToast('Appointment created.', 'success');
    } catch (err) {
      this.showToast(this.extractErrorMessage(err), 'error');
      throw err;
    }
  }

  async cancelAppointment(id: string): Promise<void> {
    await this.mutateAndReplace(id, `${APPOINTMENTS_BASE}/${id}/cancel`, 'Appointment cancelled.');
  }

  async completeAppointment(id: string): Promise<void> {
    await this.mutateAndReplace(id, `${APPOINTMENTS_BASE}/${id}/complete`, 'Appointment marked completed.');
  }

  async markNoShow(id: string): Promise<void> {
    await this.mutateAndReplace(id, `${APPOINTMENTS_BASE}/${id}/no-show`, 'Marked as no-show.');
  }

  async deleteAppointment(id: string): Promise<void> {
    try {
      await firstValueFrom(this.http.delete<void>(`${APPOINTMENTS_BASE}/${id}`).pipe(this.catchAsProblem()));
      this.appointments.update(list => list.filter(a => a.id !== id));
      if (this.selectedAppointment()?.id === id) this.closeDetailsDrawer();
      await this.refreshAvailability();
      this.showToast('Appointment deleted.', 'success');
    } catch (err) {
      this.showToast(this.extractErrorMessage(err), 'error');
      throw err;
    }
  }

  // Optimistic: caller updates the card position immediately, then calls this.
  // On failure, caller must revert using the returned original values it kept.
  async rescheduleAppointment(id: string, request: RescheduleAppointmentRequest): Promise<Appointment> {
    const dto = await firstValueFrom(
      this.http.post<ScheduleSlotDto>(`${APPOINTMENTS_BASE}/${id}/reschedule`, request).pipe(this.catchAsProblem())
    );
    const replacement = this.toAppointment(dto);
    this.appointments.update(list => [...list.filter(a => a.id !== id), replacement]);
    await this.refreshAvailability();
    return replacement;
  }

  optimisticallyMoveAppointment(id: string, newStart: Date, newEnd: Date): void {
    this.appointments.update(list =>
      list.map(a => (a.id === id ? { ...a, slotStart: newStart, slotEnd: newEnd } : a))
    );
  }

  // private async loadForCurrentSelection(doctorId: string, date: Date, view: CalendarViewMode): Promise<void> {
  //   this.loading.set(true);
  //   this.errorMessage.set(null);
  //   try {
  //     const dates = view === 'day' ? [date] : this.weekDates(date);
  //     const results = await Promise.all(
  //       dates.map(d => firstValueFrom(
  //         this.http.get<ScheduleSlotDto[]>(`${APPOINTMENTS_BASE}/doctor/${doctorId}`, {
  //           params: { date: d.toISOString() }
  //         }).pipe(this.catchAsProblem())
  //       ))
  //     );
  //     this.appointments.set(results.flat().map(dto => this.toAppointment(dto)));
  //     await this.refreshAvailability();
  //   } catch (err) {
  //     this.errorMessage.set(this.extractErrorMessage(err));
  //   } finally {
  //     this.loading.set(false);
  //   }
  // }

  // private async refreshAvailability(): Promise<void> {
  //   const doctorId = this.selectedDoctorId();
  //   const date = this.selectedDate();
  //   if (!doctorId) return;
  //   const dtos = await firstValueFrom(
  //     this.http.get<AvailableIntervalDto[]>(`${APPOINTMENTS_BASE}/doctor/${doctorId}/availability`, {
  //       params: { date: date.toISOString() }
  //     }).pipe(this.catchAsProblem())
  //   );
  //   this.availability.set(dtos.map(d => ({ start: new Date(d.start), end: new Date(d.end) })));
  // }


// schedule-page.service.ts — loadForCurrentSelection, updated to use the range endpoint
// for week view instead of looping single-day availability calls

// schedule-page.service.ts — guard against the 404-when-no-schedule behavior
private async loadForCurrentSelection(doctorId: string, date: Date, view: CalendarViewMode): Promise<void> {
  this.loading.set(true);
  this.errorMessage.set(null);

  try {
    const dates = view === 'day' ? [date] : this.weekDates(date);
    const rangeStart = dates[0];
    const rangeEnd = dates[dates.length - 1];

    const appointmentsPromise = Promise.all(dates.map(d => firstValueFrom(
      this.http.get<ScheduleSlotDto[]>(`${APPOINTMENTS_BASE}/doctor/${doctorId}`, {
        params: { date: toIsoWithOffset(d) }
      }).pipe(this.catchAsProblem())
    )));

    const availabilityPromise = this.fetchAvailabilityRange(doctorId, rangeStart, rangeEnd).catch(err => {
      if (err instanceof HttpErrorResponse && err.status === 404) return []; // no active schedule — same meaning as "no availability"
      throw err;
    });

    const [appointmentResults, dailyAvailability] = await Promise.all([appointmentsPromise, availabilityPromise]);

    this.appointments.set(appointmentResults.flat().map(dto => this.toAppointment(dto)));
    this.availability.set(dailyAvailability.flatMap(d => d.intervals));
  } catch (err) {
    this.errorMessage.set(this.extractErrorMessage(err));
  } finally {
    this.loading.set(false);
  }
}
// refreshAvailability (called after every mutation) — same range-based fix
private async refreshAvailability(): Promise<void> {
  const doctorId = this.selectedDoctorId();
  if (!doctorId) return;

  const dates = this.currentView() === 'day' ? [this.selectedDate()] : this.weekDates(this.selectedDate());
  const daily = await this.fetchAvailabilityRange(doctorId, dates[0], dates[dates.length - 1]);
  this.availability.set(daily.flatMap(d => d.intervals));
}
  private async loadWorkingSchedule(doctorId: string): Promise<void> {
    try {
      const dto = await firstValueFrom(
        this.http.get<WorkingScheduleDto>(`${WORKING_SCHEDULES_BASE}/doctor/${doctorId}`).pipe(this.catchAsProblem())
      );
      this.workingDays.set(dto.days.map(d => ({ day: d.day, startTime: d.startTime, endTime: d.endTime })));
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 404) {
        this.workingDays.set([]);
        return;
      }
      this.errorMessage.set(this.extractErrorMessage(err));
    }
  }

  private async mutateAndReplace(id: string, url: string, successMsg: string): Promise<void> {
    try {
      const dto = await firstValueFrom(this.http.post<ScheduleSlotDto>(url, {}).pipe(this.catchAsProblem()));
      const updated = this.toAppointment(dto);
      this.appointments.update(list => list.map(a => (a.id === id ? updated : a)));
      await this.refreshAvailability();
      if (this.selectedAppointment()?.id === id) this.selectedAppointment.set(updated);
      this.showToast(successMsg, 'success');
    } catch (err) {
      this.showToast(this.extractErrorMessage(err), 'error');
      throw err;
    }
  }

  private weekDates(anchor: Date): Date[] {
    const start = new Date(anchor);
    start.setDate(start.getDate() - start.getDay());
    return Array.from({ length: 7 }, (_, i) => {
      const d = new Date(start);
      d.setDate(d.getDate() + i);
      return d;
    });
  }

  private windowDurationMinutes(w: WorkingDayWindow): number {
    const [sh, sm] = w.startTime.split(':').map(Number);
    const [eh, em] = w.endTime.split(':').map(Number);
    return (eh * 60 + em) - (sh * 60 + sm);
  }

  private toAppointment(dto: ScheduleSlotDto): Appointment {
    return {
      id: dto.id,
      doctorId: dto.doctorId,
      patientId: dto.patientId,
      slotStart: new Date(dto.slotStart),
      slotEnd: new Date(dto.slotEnd),
      status: dto.status
    };
  }

// private catchAsProblem<T>() {
//   return catchError((err: HttpErrorResponse) => throwError(() => err));
// }

// private catchAsProblem<T>() {
//   return catchError((err: HttpErrorResponse): Observable<never> =>
//     throwError(() => err)
//   );
// }

private catchAsProblem<T>(): MonoTypeOperatorFunction<T> {
  return catchError((err: HttpErrorResponse) => {
    return throwError(() => err);
  });
}

  private extractErrorMessage(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const problem = err.error as ProblemDetails | undefined;
      return problem?.detail ?? problem?.title ?? 'Something went wrong. Please try again.';
    }
    return 'Something went wrong. Please try again.';
  }
}

// schedule.models.ts — add this helper, remove all .toISOString() calls to the backend

// Formats a Date as an ISO 8601 string WITH the local timezone offset
// (e.g. "2026-07-07T21:00:00+03:00"), not UTC "Z". Required because the
// backend now models SlotStart/SlotEnd as DateTimeOffset and must receive
// the real offset the receptionist entered the time in — never manually
// shift hours, and never use toISOString(), which silently converts to UTC.
export function toIsoWithOffset(date: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');

  const offsetMinutes = -date.getTimezoneOffset(); // JS returns inverted sign
  const sign = offsetMinutes >= 0 ? '+' : '-';
  const absMinutes = Math.abs(offsetMinutes);
  const offsetHours = pad(Math.floor(absMinutes / 60));
  const offsetMins = pad(absMinutes % 60);

  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}` +
    `${sign}${offsetHours}:${offsetMins}`
  );
}



