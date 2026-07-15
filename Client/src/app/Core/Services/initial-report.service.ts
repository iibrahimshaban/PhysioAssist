import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

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
  patientCategory?: number;
}

export interface AiInitialReportRequest {
  patientId: string;
  text: string;
}

export interface AiInitialReportResponse {
  reply: string;
}

@Injectable({ providedIn: 'root' })
export class InitialReportService {
  private readonly baseUrl = `${environment.apiUrl}InitialReport`;

  constructor(private readonly http: HttpClient) {}

  getReportById(reportId: string) {
    return this.http.get<InitialReportResponse>(`${this.baseUrl}/${reportId}`);
  }

  getReportByPatientId(patientId: string) {
    return this.http.get<InitialReportResponse>(`${this.baseUrl}/patient/${patientId}`);
  }

  createReport(request: CreateInitialReportRequest) {
    return this.http.post<InitialReportResponse>(this.baseUrl, request);
  }

  updateReportText(reportId: string, request: UpdateReportTextRequest) {
    return this.http.put<InitialReportResponse>(`${this.baseUrl}/${reportId}/text`, request);
  }

  uploadAttachment(reportId: string, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ReportAttachmentResponse>(`${this.baseUrl}/${reportId}/attachments`, formData);
  }

  deleteAttachment(reportId: string, attachmentId: string) {
    return this.http.delete<void>(`${this.baseUrl}/${reportId}/attachments/${attachmentId}`);
  }

  getIntakeDataSummaryByPatientId(patientId: string) {
    return this.http.get<PatientIntakeSummaryResponse>(`${this.baseUrl}/patient/${patientId}/summary`);
  }

  sendChatMessage(request: AiInitialReportRequest) {
    return this.http.post<AiInitialReportResponse>(`${environment.apiUrl}ai/initial-report`, request);
  }
}
