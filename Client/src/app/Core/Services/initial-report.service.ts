import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface AiInitialReportRequest {
  patientId: number;
  text: string;
}

export interface AiInitialReportResponse {
  reply: string;
}

export interface InitialReportPayload {
  examination: string;
  initialReport: string;
  treatmentPlan: string;
  chat: Array<{ from: 'user' | 'assistant'; text: string; time?: string }>;
  attachments: string[];
}

@Injectable({ providedIn: 'root' })
export class InitialReportService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  sendChatMessage(request: AiInitialReportRequest) {
    return this.http.post<AiInitialReportResponse>(`${this.baseUrl}ai/initial-report`, request);
  }

  saveDraft(payload: InitialReportPayload) {
    return this.http.post(`${this.baseUrl}patient/initial-report/draft`, payload);
  }

  submit(payload: InitialReportPayload) {
    return this.http.post(`${this.baseUrl}patient/initial-report/submit`, payload);
  }

  getInitialReport(patientId: number) {
    return this.http.get<InitialReportPayload & {
      patientName?: string;
      patientBadge?: string;
      chiefComplaint?: string;
      injury?: string;
      dateOfInjury?: string;
    }>(`${this.baseUrl}patient/initial-report/${patientId}`);
  }
}
