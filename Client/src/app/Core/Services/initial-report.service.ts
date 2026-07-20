import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

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

@Injectable({ providedIn: 'root' })
export class InitialReportService {
  // Matches [Route("api/[controller]")] on InitialReportController -> api/InitialReport
  private readonly baseUrl = `${environment.apiUrl}InitialReport`;

  constructor(private readonly http: HttpClient) {}

  /** GET api/InitialReport/patient/{patientId}/intake — the raw PreVisitIntake
   *  row for this patient, in case the frontend needs the full formSubmissionData
   *  / painPointsData JSON rather than just the condensed summary below. */
  getIntakeDataByPatientId(patientId: string) {
    return this.http.get<PreVisitIntakeDataResponse>(`${this.baseUrl}/patient/${patientId}/intake`);
  }

  /** GET api/InitialReport/patient/{patientId}/summary — condensed intake
   *  fields (name/gender/age/chief complaint/etc.) for the patient header. */
  getIntakeDataSummaryByPatientId(patientId: string) {
    return this.http.get<PatientIntakeSummaryResponse>(`${this.baseUrl}/patient/${patientId}/summary`);
  }

  /** GET api/InitialReport/patient/{patientId} — existing report for this
   *  patient, if one was already created. 404 means none exists yet. */
  getReportByPatientId(patientId: string) {
    return this.http.get<InitialReportResponse>(`${this.baseUrl}/patient/${patientId}`);
  }

  /** GET api/InitialReport/{id} — fetch by the report's own id. */
  getReportById(reportId: string) {
    return this.http.get<InitialReportResponse>(`${this.baseUrl}/${reportId}`);
  }

  /** POST api/InitialReport — CreateInitialReportRequest(PatientId, ReportText?) */
  createReport(request: CreateInitialReportRequest) {
    return this.http.post<InitialReportResponse>(this.baseUrl, request);
  }

  /** PUT api/InitialReport/{id}/text — UpdateReportTextRequest(ReportText) */
  updateReportText(reportId: string, request: UpdateReportTextRequest) {
    return this.http.put<InitialReportResponse>(`${this.baseUrl}/${reportId}/text`, request);
  }

  /** POST api/InitialReport/{id}/submit — finalizes the report and triggers
   *  treatment-plan PDF generation on the backend. */
  submitReport(reportId: string) {
    return this.http.post<InitialReportResponse>(`${this.baseUrl}/${reportId}/submit`, {});
  }

  /** POST api/InitialReport/{id}/attachments — multipart file upload. Requires
   *  a report to already exist, so attachments can't be sent before the
   *  report's first save. */
  uploadAttachment(reportId: string, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ReportAttachmentResponse>(`${this.baseUrl}/${reportId}/attachments`, formData);
  }

  /** DELETE api/InitialReport/{id}/attachments/{attachmentId} */
  deleteAttachment(reportId: string, attachmentId: string) {
    return this.http.delete<void>(`${this.baseUrl}/${reportId}/attachments/${attachmentId}`);
  }

  /** POST api/InitialReport/{id}/transcribe — multipart audioFile + optional
   *  languageHint query param. Returns the report with reportText updated
   *  from the transcription/refinement pipeline. */
  transcribeAudio(reportId: string, audioBlob: Blob, languageHint?: string) {
    const formData = new FormData();
    formData.append('audioFile', audioBlob, 'voice-recording.webm');
    const query = languageHint ? `?languageHint=${encodeURIComponent(languageHint)}` : '';
    return this.http.post<InitialReportResponse>(`${this.baseUrl}/${reportId}/transcribe${query}`, formData);
  }
}