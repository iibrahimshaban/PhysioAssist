// --- Intake data (read-only, for populating the patient header) --------
export interface PreVisitIntakeDataResponse {
  id: string;
  doctorId: string;
  formSchemaId: string;
  formSchemaVersion: number;
  formSubmissionData: string;
  painPointsData?: string;
  status: number;
  convertedToPatientId?: string;
  submittedAt: string;
  reviewedAt?: string;
  reviewedByDoctorId?: string;
}

export interface ReportAttachmentResponse {
  id: string;
  fileUrl: string;
  fileType: string;
  fileName: string;
}

export interface InitialReportResponse {
  id: string;
  doctorId: string;
  patientId: string;
  reportText: string;
  treatmentPlanPdfUrl?: string;
  createdAt: string;
  attachments: ReportAttachmentResponse[];
}

export interface CreateInitialReportRequest {
  patientId: string;
  reportText?: string;
}

export interface UpdateReportTextRequest {
  reportText: string;
}

export interface PatientIntakeSummaryResponse {
  patientFullName?: string;
  gender?: string;
  age?: number;
  chiefComplaint?: string;
  injuryDescription?: string;
  injuryDate?: string;
  patientCategory?: number; // enum ordinal from backend
}

// --- Schedule Requirements section --------------------------------------
 
export enum PreferredTimeOfDay {
  Unspecified = 0,
  Morning = 1,
  Afternoon = 2,
  Evening = 3,
}
 
// [Flags] enum on the backend — values are bitmask-combinable.
export enum DaysOfWeekFlags {
  None = 0,
  Sunday = 1,
  Monday = 2,
  Tuesday = 4,
  Wednesday = 8,
  Thursday = 16,
  Friday = 32,
  Saturday = 64,
}
 
export enum SchedulingPriority {
  Normal = 0,
  Low = 1,
  High = 2,
  Urgent = 3,
}
 
export enum TreatmentSchedulePlanStatus {
  Pending = 0,
  SentToReceptionist = 1,
  Booked = 2,
}
 
export enum SlotFitType {
  Exact = 0,
  LongerThanRequested = 1,
  ShorterThanRequested = 2,
}
 
export interface SlotCandidateDto {
  start: string;               // ISO DateTimeOffset
  end: string;                 // ISO DateTimeOffset
  availableDuration: string;   // "hh:mm:ss" — confirm TimeSpan serializes this way on your .NET version
  requestedDuration: string;
  fitType: SlotFitType;
  gap: string;
  isBeyondPreferredHorizon: boolean;
  score: number;
}
 
export interface UpsertTreatmentSchedulePlanRequest {
  totalSessions: number;
  sessionDurationMinutes: number;
  sessionsPerWeek: number;
  minimumGapBetweenSessionsDays: number;
  preferredTimeOfDay: PreferredTimeOfDay;
  preferredDays: DaysOfWeekFlags;
  priority: SchedulingPriority;
}
 
export interface BookTreatmentSlotRequest {
  slotStart: string; // ISO DateTimeOffset
  slotEnd: string;
}
 
export interface TreatmentSchedulePlanResponse {
  id: string;
  reportId: string;
  totalSessions: number;
  sessionDurationMinutes: number;
  sessionsPerWeek: number;
  minimumGapBetweenSessionsDays: number;
  preferredTimeOfDay: PreferredTimeOfDay;
  preferredDays: DaysOfWeekFlags;
  priority: SchedulingPriority;
  status: TreatmentSchedulePlanStatus;
  packageId?: string;
  candidateSlots: SlotCandidateDto[];
}