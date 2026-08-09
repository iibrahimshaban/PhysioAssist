import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { DocumentationField, DocumentationTemplateSummary, PatientCategory } from '../../Shared/Models/documentation.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class DocumentationTemplateService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}DocumentationTemplate`;
 
  getTemplates(category?: PatientCategory): Observable<DocumentationTemplateSummary[]> {
    let params = new HttpParams();
    if (category !== undefined) {
      params = params.set('category', category);
    }
    return this.http.get<DocumentationTemplateSummary[]>(this.baseUrl, { params });
  }

  getAllFields(templateId: string): Observable<DocumentationField[]> {
    return this.http.get<DocumentationField[]>(`${this.baseUrl}/${templateId}/fields`);
  }
 
  getEffectiveFields(templateId: string): Observable<DocumentationField[]> {
    return this.http.get<DocumentationField[]>(`${this.baseUrl}/${templateId}/effective-fields`);
  }
 
  saveHiddenFields(templateId: string, hiddenFieldIds: string[]): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${templateId}/hidden-fields`, { hiddenFieldIds });
  }
}
