export enum IntakeStatus {
  Pending = 0,
  Submitted = 1,
  InReview = 2,
  Approved = 3,
  Rejected = 4,
  Converted = 5,
  Expired = 6
}

export interface PreVisitIntakeResponse {
  id: string;
  doctorId: string;
  formSchemaId: string;
  formSchemaVersion: number;
  patientName: string;
  patientEmail?: string;
  patientPhone?: string;
  status: IntakeStatus;
  convertedToPatientId?: string;
  submittedAt: string;
  reviewedAt?: string;
  reviewedByDoctorId?: string;
}

export interface PreVisitIntakeDetailsResponse {
  id: string;
  doctorId: string;
  formSchemaId: string;
  formSchemaVersion: number;
  patientName: string;
  patientEmail?: string;
  patientPhone?: string;
  formSubmissionData: string;
  painPointsData?: string;
  status: IntakeStatus;
  convertedToPatientId?: string;
  submittedAt: string;
  reviewedAt?: string;
  reviewedByDoctorId?: string;
  formSchemaName: string;
}

export interface SubmitPreVisitIntakeRequest {
  patientName: string;
  patientEmail?: string;
  patientPhone?: string;
  formSubmissionData: string;
  painPointsData?: string;
}

export interface UpdateIntakeStatusRequest {
  newStatus: IntakeStatus;
}

export interface ConvertIntakeToPatientRequest {
  notes?: string;
}
