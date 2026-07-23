import {
  DaysOfWeekFlags,
  PreferredTimeOfDay,
  SchedulingPriority,
  SlotCandidateDto,
} from '../../Shared/Models/InitialReport.models';

import { ScheduleSlotDto } from '../../Features/Schedule/schedule.models';

export interface ReceptionistCreateSessionPackageRequest {
  patientId: string;
  totalSessions: number;
  sessionDuration: string; // TimeSpan -> "hh:mm:ss"
  sessionsPerWeek: number;
  minimumGapBetweenSessionsDays: number;
  preferredTimeOfDay: PreferredTimeOfDay;
  preferredDays: DaysOfWeekFlags;
  priority: SchedulingPriority;
  firstSessionSlot?: SlotCandidateDto;
}

// Mirrors CreateSessionPackageResult.
export interface CreateSessionPackageResult {
  packageId: string;
  scheduledSessions: number;
  firstSessionSlot: ScheduleSlotDto | null;
}

// Mirrors SessionBookingRoundDto.
export interface SessionBookingRoundDto {
  packageId: string;
  sessionNumber: number;
  totalSessions: number;
  remainingSessions: number;
  weeklyTargetCount: number;
  scheduledThisWeek: number;
  weekStart: string;
  weekEnd: string;
  weeklyQuotaMet: boolean;
  noRoomLeftThisWeek: boolean;
  candidates: SlotCandidateDto[];
  patientFreeTimeText: string; // ADD
}

export interface GetNextSessionCandidatesRequest {
  patientFreeTimeOverride?: string | null;
  persistFreeTimeOverride: boolean;
}

export interface PatientSessionPackageSummaryDto {
  packageId: string;
  patientId: string;
  doctorId: string;
  totalSessions: number;
  scheduledSessions: number;
  remainingSessions: number;
  nextSessionNumber: number;
  status: PackageStatus;
  sessionsPerWeek: number;
  sessionDuration: string;
  patientFreeTimeText: string;
}

export enum PackageStatus {
  Active = 0,
  Completed = 1,
  Cancelled = 2
}

export enum PatientSchedulingState {
  NoInitialReport = 0,
  PlanPending = 1,
  ReadyToSchedule = 2,
  ActivePackage = 3,
}
 
// Mirrors PendingTreatmentPlanDto — the doctor's own plan, shown to the
// receptionist read-only before she converts it into a real bookable package.
export interface PendingTreatmentPlanDto {
  treatmentPlanId: string;
  reportId: string;
  totalSessions: number;
  sessionDurationMinutes: number;
  sessionsPerWeek: number;
  minimumGapBetweenSessionsDays: number;
  preferredTimeOfDay: PreferredTimeOfDay;
  preferredDays: DaysOfWeekFlags;
  priority: SchedulingPriority;
}
 
// Mirrors PatientSchedulingContextDto — the single source of truth for "what
// should this patient's scheduling screen show right now."
export interface PatientSchedulingContextDto {
  state: PatientSchedulingState;
  pendingPlan?: PendingTreatmentPlanDto | null;
  activePackage?: PatientSessionPackageSummaryDto | null;
}
 
// Mirrors ConvertPlanToPackageRequest — all fields optional overrides; omitting
// a field means "use whatever the doctor's plan already specified."
export interface ConvertPlanToPackageRequest {
  sessionsPerWeek?: number | null;
  minimumGapBetweenSessionsDays?: number | null;
  preferredTimeOfDay?: PreferredTimeOfDay | null;
  preferredDays?: DaysOfWeekFlags | null;
  priority?: SchedulingPriority | null;
}