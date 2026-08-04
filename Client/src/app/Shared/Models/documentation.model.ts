export interface DocumentationField {
  id: string;
  label?: string;
  type?: string;
  [key: string]: unknown; // schema-driven — other props vary by field type
}

export enum PatientCategory {
  Orthopedic = 0,
  Neurological = 1,
  Pediatric = 2,
  GeneralOther = 3
}
 
export const PATIENT_CATEGORY_LABELS: Record<PatientCategory, string> = {
  [PatientCategory.Orthopedic]: 'Orthopedic',
  [PatientCategory.Neurological]: 'Neurological',
  [PatientCategory.Pediatric]: 'Pediatric',
  [PatientCategory.GeneralOther]: 'General / Other'
};
 
export interface DocumentationTemplateSummary {
  id: string;
  name: string;
  category: PatientCategory;
}
 
export interface SessionProgressNote {
  id: string;
  sessionId: string;
  documentationTemplateId: string;
  subjective: string;
  objectiveFindings: string | null; // raw JSON string, shape driven by the DocumentationTemplate
  assessment: string;
  plan: string;
}
 
export interface NarrativeDraft {
  subjective: string;
  assessment: string;
  plan: string;
}
 
export interface GenerateAiSummaryResponse {
  progressNote: SessionProgressNote;
  narrativeDraft: NarrativeDraft | null; // null if narrative drafting failed but Objective succeeded
}

export enum SlotStatus {
  Booked = 0,
  Completed = 1,
  Cancelled = 2,
  NoShow = 3
}

export const SLOT_STATUS_META: Record<SlotStatus, { label: string; badgeClass: string }> = {
  [SlotStatus.Booked]: { label: 'Booked', badgeClass: 'bg-blue-100 text-blue-700' },
  [SlotStatus.Completed]: { label: 'Completed', badgeClass: 'bg-green-100 text-green-700' },
  [SlotStatus.Cancelled]: { label: 'Cancelled', badgeClass: 'bg-gray-100 text-gray-600' },
  [SlotStatus.NoShow]: { label: 'No Show', badgeClass: 'bg-red-100 text-red-700' }
};

export interface PatientDocumentationSession {
  sessionId: string;
  date: string; // ISO DateTimeOffset
  durationMinutes: number;
  attendanceStatus: SlotStatus;
  hasProgressNote: boolean;
  hasSummary: boolean;
  isSummaryStale: boolean;
  narrativeSummary: string | null;
}

export enum SummaryAudience {
  Colleague = 0,
  Patient = 1
}

export enum SummaryScope {
  Full = 0,
  Partial = 1,
  Focused = 2
}

export interface GenerateDocumentationSummaryRequest {
  patientId: string;
  audience: SummaryAudience;
  scope?: SummaryScope;
  focusAreas?: string[];
}

export interface DocumentationSummaryResponse {
  id: string;
  audience: SummaryAudience;
  scope: SummaryScope | null;
  focusAreas: string[] | null;
  anonymizePersonalData: boolean;
  summaryText: string;
  fileUrl: string;
  generatedAt: string;
}