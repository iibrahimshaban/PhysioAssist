import { PackageStatus } from "../../Features/receptionist-scheduling/SessionScheduling.model";

export enum SlotStatus {
  Booked = 0,
  Completed = 1,
  Cancelled = 2,
  NoShow = 3,
}

export interface PatientSessionListItemDto {
  slotId: string;
  sessionNumber: number;
  slotStart: string;
  slotEnd: string;
  status: SlotStatus;
}

export interface PatientScheduleOverviewDto {
  hasPackage: boolean;
  packageId: string | null;
  packageStatus: PackageStatus | null;
  totalSessions: number;
  completedSessions: number;
  remainingSessions: number;
  upcomingScheduledCount: number;
  sessions: PatientSessionListItemDto[];
}