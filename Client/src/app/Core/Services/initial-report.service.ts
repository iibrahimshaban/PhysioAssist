import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { BookTreatmentSlotRequest, CreateInitialReportRequest, InitialReportResponse, PatientIntakeSummaryResponse, PreVisitIntakeDataResponse, ReportAttachmentResponse, TreatmentSchedulePlanResponse, UpdateReportTextRequest, UpsertTreatmentSchedulePlanRequest } from '../../Shared/Models/InitialReport.models';

@Injectable({ providedIn: 'root' })
export class InitialReportService {
  private readonly baseUrl = `${environment.apiUrl}InitialReport`;

  constructor(private readonly http: HttpClient) {}

  getIntakeDataByPatientId(patientId: string) {
    return this.http.get<PreVisitIntakeDataResponse>(`${this.baseUrl}/patient/${patientId}/intake`);
  }

  getIntakeDataSummaryByPatientId(patientId: string) {
    return this.http.get<PatientIntakeSummaryResponse>(`${this.baseUrl}/patient/${patientId}/summary`);
  }

  getReportByPatientId(patientId: string) {
    return this.http.get<InitialReportResponse>(`${this.baseUrl}/patient/${patientId}`);
  }

  getReportById(reportId: string) {
    return this.http.get<InitialReportResponse>(`${this.baseUrl}/${reportId}`);
  }

  createReport(request: CreateInitialReportRequest) {
    return this.http.post<InitialReportResponse>(this.baseUrl, request);
  }

  updateReportText(reportId: string, request: UpdateReportTextRequest) {
    return this.http.put<InitialReportResponse>(`${this.baseUrl}/${reportId}/text`, request);
  }

  submitReport(reportId: string) {
    return this.http.post<InitialReportResponse>(`${this.baseUrl}/${reportId}/submit`, {});
  }

  uploadAttachment(reportId: string, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ReportAttachmentResponse>(`${this.baseUrl}/${reportId}/attachments`, formData);
  }

  deleteAttachment(reportId: string, attachmentId: string) {
    return this.http.delete<void>(`${this.baseUrl}/${reportId}/attachments/${attachmentId}`);
  }

  transcribeAudio(reportId: string, audioBlob: Blob, languageHint?: string) {
    const formData = new FormData();
    formData.append('audioFile', audioBlob, 'voice-recording.webm');
    const query = languageHint ? `?languageHint=${encodeURIComponent(languageHint)}` : '';
    return this.http.post<InitialReportResponse>(`${this.baseUrl}/${reportId}/transcribe${query}`, formData);
  }
  
  /** POST api/InitialReport/{id}/schedule-plan — create or update the section;
 *  response always includes freshly computed candidateSlots. */
  upsertSchedulePlan(reportId: string, request: UpsertTreatmentSchedulePlanRequest) {
    return this.http.post<TreatmentSchedulePlanResponse>(`${this.baseUrl}/${reportId}/schedule-plan`, request);
  }

  /** GET api/InitialReport/{id}/schedule-plan — 404 if none exists yet for this report. */
  getSchedulePlan(reportId: string) {
    return this.http.get<TreatmentSchedulePlanResponse>(`${this.baseUrl}/${reportId}/schedule-plan`);
  }

  /** POST api/InitialReport/{id}/schedule-plan/book — doctor picked a candidate slot himself. */
  bookSchedulePlan(reportId: string, request: BookTreatmentSlotRequest) {
    return this.http.post<TreatmentSchedulePlanResponse>(`${this.baseUrl}/${reportId}/schedule-plan/book`, request);
  }

  /** POST api/InitialReport/{id}/schedule-plan/send-to-receptionist — defers booking. */
  sendSchedulePlanToReceptionist(reportId: string) {
    return this.http.post<TreatmentSchedulePlanResponse>(`${this.baseUrl}/${reportId}/schedule-plan/send-to-receptionist`, {});
  }
}