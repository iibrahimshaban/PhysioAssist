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
  treatmentPlanPdfUrl: string;
  createdAt: string;
  attachments: ReportAttachmentResponse[];
}

@Injectable({ providedIn: 'root' })
export class InitialReportService {
  private readonly baseUrl = `${environment.apiUrl}InitialReport`;

  constructor(private readonly http: HttpClient) {}

  createReport(patientId: string) {
    return this.http.post<InitialReportResponse>(this.baseUrl, {
      patientId,
      reportText: '',
    });
  }

  getReport(id: string) {
    return this.http.get<InitialReportResponse>(`${this.baseUrl}/${id}`);
  }

  updateReportText(id: string, reportText: string) {
    return this.http.put<InitialReportResponse>(`${this.baseUrl}/${id}/text`, {
      reportText,
    });
  }

  transcribeAudio(id: string, audioBlob: Blob, languageHint = 'ar') {
    const formData = new FormData();
    formData.append('audioFile', audioBlob, 'voice-recording.wav');
    return this.http.post<InitialReportResponse>(`${this.baseUrl}/${id}/transcribe?languageHint=${encodeURIComponent(languageHint)}`, formData);
  }

  uploadAttachment(id: string, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ReportAttachmentResponse>(`${this.baseUrl}/${id}/attachments`, formData);
  }

  deleteAttachment(id: string, attachmentId: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}/attachments/${attachmentId}`);
  }

  submitReport(id: string) {
    return this.http.post<InitialReportResponse>(`${this.baseUrl}/${id}/submit`, {});
  }
}
