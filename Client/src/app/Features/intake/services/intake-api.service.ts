import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  FormSchemaResponse,
  FormSchemaSummaryResponse,
  CreateFormSchemaRequest,
  UpdateFormSchemaRequest,
  PublishFormSchemaRequest,
  GenerateIntakeQrLinkRequest,
  GenerateIntakeQrLinkResponse,
  PreVisitIntakeResponse,
  PreVisitIntakeDetailsResponse,
  UpdateIntakeStatusRequest,
  ConvertIntakeToPatientRequest,
  IntakeStatus
} from '../models';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class IntakeApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}intake`;

  // Form Schema Management
  createFormSchema(request: CreateFormSchemaRequest): Observable<FormSchemaResponse> {
    return this.http.post<FormSchemaResponse>(`${this.baseUrl}/form-schemas`, request);
  }

  updateFormSchema(schemaId: string, request: UpdateFormSchemaRequest): Observable<FormSchemaResponse> {
    return this.http.put<FormSchemaResponse>(`${this.baseUrl}/form-schemas/${schemaId}`, request);
  }

  publishFormSchema(schemaId: string, request: PublishFormSchemaRequest): Observable<FormSchemaResponse> {
    return this.http.post<FormSchemaResponse>(`${this.baseUrl}/form-schemas/${schemaId}/publish`, request);
  }

  getFormSchemas(): Observable<FormSchemaSummaryResponse[]> {
    return this.http.get<FormSchemaSummaryResponse[]>(`${this.baseUrl}/form-schemas`);
  }

  getFormSchemaById(schemaId: string): Observable<FormSchemaResponse> {
    return this.http.get<FormSchemaResponse>(`${this.baseUrl}/form-schemas/${schemaId}`);
  }

  getDefaultFormSchema(): Observable<FormSchemaResponse> {
    return this.http.get<FormSchemaResponse>(`${this.baseUrl}/form-schemas/default`);
  }

  generateIntakeQrLink(schemaId: string, request: GenerateIntakeQrLinkRequest): Observable<GenerateIntakeQrLinkResponse> {
    return this.http.post<GenerateIntakeQrLinkResponse>(`${this.baseUrl}/form-schemas/${schemaId}/qr-link`, request);
  }

  // Intake Submissions Review
  getSubmissions(status?: IntakeStatus): Observable<PreVisitIntakeResponse[]> {
    let params = new HttpParams();
    if (status !== undefined) {
      params = params.set('status', status.toString());
    }
    return this.http.get<PreVisitIntakeResponse[]>(`${this.baseUrl}/submissions`, { params });
  }

  getSubmissionDetails(submissionId: string): Observable<PreVisitIntakeDetailsResponse> {
    return this.http.get<PreVisitIntakeDetailsResponse>(`${this.baseUrl}/submissions/${submissionId}`);
  }

  updateIntakeStatus(submissionId: string, request: UpdateIntakeStatusRequest): Observable<PreVisitIntakeResponse> {
    return this.http.patch<PreVisitIntakeResponse>(`${this.baseUrl}/submissions/${submissionId}/status`, request);
  }

  convertToPatient(submissionId: string, request: ConvertIntakeToPatientRequest = {}): Observable<PreVisitIntakeResponse> {
    return this.http.post<PreVisitIntakeResponse>(`${this.baseUrl}/submissions/${submissionId}/convert-to-patient`, request);
  }
}
