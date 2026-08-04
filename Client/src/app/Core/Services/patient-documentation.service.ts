import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { DocumentationSummaryResponse, GenerateDocumentationSummaryRequest, PatientDocumentationSession } from '../../Shared/Models/documentation.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class PatientDocumentationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}`;
 
  getSessions(patientId: string): Observable<PatientDocumentationSession[]> {
    return this.http.get<PatientDocumentationSession[]>(
      `${this.baseUrl}patients/${patientId}/documentation/sessions`
    );
  }
 
  generateSummary(request: GenerateDocumentationSummaryRequest): Observable<DocumentationSummaryResponse> {
    return this.http.post<DocumentationSummaryResponse>(`${this.baseUrl}documentation-summaries/generate`, request);
  }
 
  generatePdf(documentationSummaryId: string): Observable<DocumentationSummaryResponse> {
    return this.http.post<DocumentationSummaryResponse>(
      `${this.baseUrl}documentation-summaries/${documentationSummaryId}/generate-pdf`,
      {}
    );
  }
}
