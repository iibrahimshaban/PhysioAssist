export type ScheduleSlotStatus = 'Booked' | 'Completed' | 'Cancelled' | 'NoShow';
export type CalendarViewMode = 'day' | 'week';

export interface ScheduleSlotDto {
  id: string;
  doctorId: string;
  patientId: string;
  slotStart: string;
  slotEnd: string;
  status: ScheduleSlotStatus;
}

export interface AvailableIntervalDto { start: string; end: string; }

export interface CreateAppointmentRequest {
  doctorId: string;
  patientId: string;
  slotStart: string;
  slotEnd: string;
}

export interface RescheduleAppointmentRequest { newSlotStart: string; newSlotEnd: string; }

export interface WorkingScheduleDayDto { day: number; startTime: string; endTime: string; }

export interface WorkingScheduleDto {
  id: string;
  doctorId: string;
  isActive: boolean;
  days: WorkingScheduleDayDto[];
}

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
}

export interface Doctor {
  id: string;
  // TEMPORARY placeholder — no real Doctor endpoint provided yet.
  displayLabel: string;
}

export interface ScheduleFilters {
  patientSearch: string;
  statuses: Set<ScheduleSlotStatus>;
  showOnlyToday: boolean;
}

export interface ScheduleStatistics {
  appointmentsToday: number;
  completedToday: number;
  cancelledToday: number;
  noShowToday: number;
  freeMinutesRemaining: number;
  occupancyPercent: number;
  averageDurationMinutes: number;
}

export interface Appointment {
  id: string;
  doctorId: string;
  patientId: string;
  slotStart: Date;
  slotEnd: Date;
  status: ScheduleSlotStatus;
}

export interface AvailableInterval { start: Date; end: Date; }

export interface WorkingDayWindow { day: number; startTime: string; endTime: string; }

export interface ToastMessage { id: number; text: string; kind: 'success' | 'error'; }

// TEMPORARY: no real patient/doctor name source yet.
export function shortId(id: string): string {
  return id.slice(0, 8).toUpperCase();
}