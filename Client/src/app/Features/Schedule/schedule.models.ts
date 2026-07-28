export type ScheduleSlotStatus = 'Booked' | 'Completed' | 'Cancelled' | 'NoShow';
export type CalendarViewMode = 'day' | 'week';

export interface ScheduleSlotDto {
  id: string;
  doctorId: string;
  patientId: string | null;
  guestId: string | null;
  slotStart: string;
  slotEnd: string;
  status: ScheduleSlotStatus;
}
export interface AvailableIntervalDto { start: string; end: string; }

export interface CreateAppointmentRequest {
  doctorId: string;
  patientId?: string | null;
  guestId?: string | null;
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
  patientId: string | null;
  guestId: string | null;
  slotStart: Date;
  slotEnd: Date;
  status: ScheduleSlotStatus;
}

export interface CreateGuestRequest {
  fullName: string;
  phoneNumber: string;
}

export interface GuestResponse {
  id: string;
  fullName: string;
  phoneNumber: string;
}

export interface AvailableInterval { start: Date; end: Date; }

export interface WorkingDayWindow { day: number; startTime: string; endTime: string; }

export interface ToastMessage { id: number; text: string; kind: 'success' | 'error'; }

// TEMPORARY: no real patient/doctor name source yet.
export function shortId(id: string): string {
  return id.slice(0, 8).toUpperCase();
}

export function ownerLabel(a: { patientId: string | null; guestId: string | null }): string {
  if (a.guestId) return `Guest ${shortId(a.guestId)}`;
  if (a.patientId) return `Patient ${shortId(a.patientId)}`;
  return 'Unknown';
}

// schedule.models.ts — additions

export interface AvailableIntervalDto {
  start: string; // "HH:mm:ss" — time only, combine with parent DailyAvailabilityDto.date
  end: string;   // "HH:mm:ss" — time only, combine with parent DailyAvailabilityDto.date
}

export interface DailyAvailabilityDto {
  date: string; // "YYYY-MM-DD" from DateOnly
  intervals: AvailableIntervalDto[];
}
export interface DailyAvailability {
  date: Date;
  intervals: AvailableInterval[];
}




// AFTER — add startClientX, originalDayIndex, colWidth
interface DragState {
  appointment: Appointment;
  originalStart: Date;
  originalEnd: Date;
  startClientY: number;
  startClientX: number;   // NEW — needed to compute horizontal drag distance
  originalDayIndex: number; // NEW — which column the appointment started in
  colWidth: number;        // NEW — cached column width, so we don't re-measure the DOM on every pointermove
  mode: 'move' | 'resize';
}


export interface PatientResponse {
  id: string;
  fullName: string;
  dateOfBirth: string | null;
  gender: string;
  phoneNumber: string | null;
  emailAddress: string;
  occupation: string | null;
  qrCodeToken: string;
  status: string;
}

export interface PatientOption {
  id: string;
  name: string;
  isGuest: boolean;
}