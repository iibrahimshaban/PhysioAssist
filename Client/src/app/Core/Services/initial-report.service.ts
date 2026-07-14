import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

// --- AI chat -----------------------------------------------------------
// NOTE: not present on InitialReportController as shown — leaving as-is
// until confirmed which module actually owns this endpoint.
export interface AiInitialReportRequest {
  patientId: string;
  text: string;
}

export interface AiInitialReportResponse {
  reply: string;
}

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

// --- Initial report itself ---------------------------------------------
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
  patientCategory?: number; // enum ordinal from backend — see note below
}

@Injectable({ providedIn: 'root' })
export class InitialReportService {
  // Matches [Route("api/[controller]")] on InitialReportController -> api/InitialReport
  private readonly baseUrl = `${environment.apiUrl}InitialReport`;

  constructor(private readonly http: HttpClient) {}

  /** GET api/InitialReport/patient/{patientId}/intake — the raw PreVisitIntake
   *  row for this patient, so the frontend can pull name/dob/gender/chief
   *  complaint/etc. out of formSubmissionData / painPointsData itself. */
  getIntakeDataByPatientId(patientId: string) {
    return this.http.get<PreVisitIntakeDataResponse>(`${this.baseUrl}/patient/${patientId}/intake`);
  }

  /** POST api/InitialReport — CreateInitialReportRequest(PatientId, ReportText?) */
  createReport(request: CreateInitialReportRequest) {
    return this.http.post<InitialReportResponse>(this.baseUrl, request);
  }

  /** GET api/InitialReport/{id} — fetch by the report's own id, not patientId. */
  getReportById(reportId: string) {
    return this.http.get<InitialReportResponse>(`${this.baseUrl}/${reportId}`);
  }

  /** PUT api/InitialReport/{id}/text — UpdateReportTextRequest(ReportText) */
  updateReportText(reportId: string, request: UpdateReportTextRequest) {
    return this.http.put<InitialReportResponse>(`${this.baseUrl}/${reportId}/text`, request);
  }

  /** POST api/InitialReport/{id}/attachments — multipart file upload. Requires
   *  a report to already exist (its id), so attachments can't be sent before
   *  the report's first save. */
  uploadAttachment(reportId: string, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ReportAttachmentResponse>(`${this.baseUrl}/${reportId}/attachments`, formData);
  }

  /** DELETE api/InitialReport/{id}/attachments/{attachmentId} */
  deleteAttachment(reportId: string, attachmentId: string) {
    return this.http.delete<void>(`${this.baseUrl}/${reportId}/attachments/${attachmentId}`);
  }
  getReportByPatientId(patientId: string) {
    return this.http.get<InitialReportResponse>(`${this.baseUrl}/patient/${patientId}`);
  }
  getIntakeDataSummaryByPatientId(patientId: string) {
    return this.http.get<PatientIntakeSummaryResponse>(`${this.baseUrl}/patient/${patientId}/summary`);
  }

  // --- Not yet wired into the component -----------------------------
  // POST api/InitialReport/{id}/transcribe (multipart audioFile + optional
  // languageHint query param). Response shape (result.Value) isn't confirmed
  // yet, and this also needs browser-side audio capture (MediaRecorder) to
  // replace the current SpeechRecognition-based voice input. Left as a stub
  // until both are confirmed.
  // transcribe(reportId: string, audioFile: Blob, languageHint?: string) { ... }

  /** @deprecated superseded by createReport/updateReportText */
  sendChatMessage(request: AiInitialReportRequest) {
    return this.http.post<AiInitialReportResponse>(`${environment.apiUrl}ai/initial-report`, request);
  }
}