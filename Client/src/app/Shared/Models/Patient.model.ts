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

export enum PatientStatus {
  // TODO: confirm these values/order against the backend PatientStatus enum
  Pending = 0,
  Active = 1,
  Discharged = 2,
  Inactive = 3,
}

export interface PatientRequest {
  fullName: string;
  dateOfBirth: string; // ISO date string
  gender: string;
  phoneNumber: string;
  emailAddress: string;
}

export interface PatientResponse {
  id: string;
  fullName: string;
  dateOfBirth: string;
  gender: string;
  phoneNumber: string;
  emailAddress: string;
  qrCodeToken: string;
  status: PatientStatus;
}

export interface PatientWithNextSlotResponse {
  id: string;
  fullName: string;
  phoneNumber: string;
  emailAddress: string;
  gender: string;
  status: PatientStatus;
  qrCodeToken: string;
  slotStart: string | null;
  slotEnd: string | null;
}

export interface PatientOverviewResponse {
  id: string;
  fullName: string;
  dateOfBirth: string;
  gender: string;
  phoneNumber: string;
  emailAddress: string;
  status: PatientStatus;
  formSubmissionData: string | null; // raw JSON string — parse when consumed
  painPointsJson: string | null;     // raw JSON string — { regions }
  doctorInfoJson: string | null;     // raw JSON string — { chiefComplaint, patientCategory }
}

export interface UpdateSubmissionDataRequest {
  formSubmissionData: string;
  painPointsData?: string;
}

// Not currently called from PatientService — likely belongs to a schedule-slot service instead
export interface MarkNoShowRequest {
  countsAsUsed: boolean;
}

export interface CreateDirectIntakeRequest {
  formSchemaId: string;       // Guid
  formSubmissionData: string; // raw JSON string — untouched, whatever the dynamic form produced
  painPointsData?: string;    // raw JSON string — untouched
}