// Mirrors PhysioAssist.Api.Modules.Scheduling.DTO
// .NET DayOfWeek enum: Sunday = 0, Monday = 1, ... Saturday = 6

export type DayOfWeekValue = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export interface DayOption {
  value: DayOfWeekValue;
  label: string;
}

/** Fixed display order, Sunday -> Saturday, matching the backend enum values. */
export const WEEK_DAYS: readonly DayOption[] = [
  { value: 0, label: 'Sunday' },
  { value: 1, label: 'Monday' },
  { value: 2, label: 'Tuesday' },
  { value: 3, label: 'Wednesday' },
  { value: 4, label: 'Thursday' },
  { value: 5, label: 'Friday' },
  { value: 6, label: 'Saturday' },
];

export interface WorkingScheduleDayDto {
  day: DayOfWeekValue;
  startTime: string; // "HH:mm:ss"
  endTime: string; // "HH:mm:ss"
}

export interface WorkingScheduleDto {
  id: string;
  doctorId: string;
  isActive: boolean;
  days: WorkingScheduleDayDto[];
}

// ASSUMPTION: WorkingScheduleDayRequest wasn't shown, assumed to mirror
// WorkingScheduleDayDto's shape (day / startTime / endTime). Adjust if different.
export interface WorkingScheduleDayRequest {
  day: DayOfWeekValue;
  startTime: string;
  endTime: string;
}

export interface CreateWorkingScheduleRequest {
  days: WorkingScheduleDayRequest[];
}

export interface UpdateWorkingScheduleDaysRequest {
  days: WorkingScheduleDayRequest[];
}
