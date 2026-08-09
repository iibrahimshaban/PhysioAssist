import { Injectable } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { BookTreatmentSlotRequest, CreateInitialReportRequest, InitialReportResponse, PatientIntakeSummaryResponse, PreVisitIntakeDataResponse, ReportAttachmentResponse, TranscriptionResponse, TreatmentSchedulePlanResponse, UpdateReportTextRequest, UpsertTreatmentSchedulePlanRequest } from '../../Shared/Models/InitialReport.models';
import { SKIP_ERROR_SNACKBAR } from '../Interceptors/skip-error-interceptor.token';

@Injectable({ providedIn: 'root' })
export class InitialReportService {


  private readonly baseUrl = `${environment.apiUrl}InitialReport`;

  constructor(private readonly http: HttpClient) {}

  getIntakeDataByPatientId(patientId: string) {
    return this.http.get<PreVisitIntakeDataResponse>(`${this.baseUrl}/patient/${patientId}/intake`);
  }

  getIntakeDataSummaryByPatientId(patientId: string) {
    // 404 = intake not filled yet; loadIntakeHeader already handles this locally.
    return this.http.get<PatientIntakeSummaryResponse>(`${this.baseUrl}/patient/${patientId}/summary`, {
      context: new HttpContext().set(SKIP_ERROR_SNACKBAR, true),
    });
  }

  getReportByPatientId(patientId: string) {
    // 404 = no report yet; loadOrCreateReport creates one on 404, that's expected.
    return this.http.get<InitialReportResponse>(`${this.baseUrl}/patient/${patientId}`, {
      context: new HttpContext().set(SKIP_ERROR_SNACKBAR, true),
    });
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
    return this.http.post<TranscriptionResponse>(
      `${this.baseUrl}/${reportId}/transcribe${query}`,
      formData,
    );
  }

  upsertSchedulePlan(reportId: string, request: UpsertTreatmentSchedulePlanRequest) {
    return this.http.post<TreatmentSchedulePlanResponse>(
      `${this.baseUrl}/${reportId}/schedule-plan`,
      request
    );
  }


  /** GET api/InitialReport/{id}/schedule-plan — 404 if none exists yet for this report. */
  getSchedulePlan(reportId: string) {
    return this.http.get<TreatmentSchedulePlanResponse>(`${this.baseUrl}/${reportId}/schedule-plan`, {
      context: new HttpContext().set(SKIP_ERROR_SNACKBAR, true),
    });
  }

  bookSchedulePlan(reportId: string, request: BookTreatmentSlotRequest) {
    return this.http.post<TreatmentSchedulePlanResponse>(
      `${this.baseUrl}/${reportId}/schedule-plan/book`,
      request
    );
  }

  sendSchedulePlanToReceptionist(reportId: string) {
    return this.http.post<TreatmentSchedulePlanResponse>(
      `${this.baseUrl}/${reportId}/schedule-plan/send-to-receptionist`,
      {}
    );
  }
}