// next-session-booking.model.ts
export enum NextSessionBookingState {
  NotApplicable = 0,
  CanBookNext = 1,
  LastSessionDecisionNeeded = 2,
}

export interface NextSessionBookingContextDto {
  state: NextSessionBookingState;
  packageId: string | null;
  nextScheduledSlotStart: string | null;
}

export interface GetNextSessionCandidatesRequest {
  sessionDurationOverride?: string | null; // "HH:mm:ss" — .NET TimeSpan JSON format
  sessionsPerWeekOverride?: number | null;
  minimumGapOverrideDays?: number | null;
}

export interface SlotCandidateDto {
  start: string;
  end: string;
}

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
  patientFreeTimeText: string;
}