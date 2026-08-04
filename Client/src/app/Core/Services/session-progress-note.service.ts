import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GenerateAiSummaryResponse, NarrativeDraft, SessionProgressNote } from '../../Shared/Models/documentation.model';
import { SKIP_ERROR_SNACKBAR } from '../Interceptors/skip-error-interceptor.token';

@Injectable({
  providedIn: 'root',
})
export class SessionProgressNoteService {
  private readonly http = inject(HttpClient);

  private baseUrl(sessionId: string): string {
    return `${environment.apiUrl}sessions/${sessionId}/progress-note`;
  }

  get(sessionId: string): Observable<SessionProgressNote> {
    return this.http.get<SessionProgressNote>(this.baseUrl(sessionId), {
      context: new HttpContext().set(SKIP_ERROR_SNACKBAR, true),
    });
  }

  updateNarrative(
    sessionId: string,
    body: { subjective: string; assessment: string; plan: string }
  ): Observable<SessionProgressNote> {
    return this.http.put<SessionProgressNote>(this.baseUrl(sessionId), body);
  }

  generateAiSummary(sessionId: string): Observable<GenerateAiSummaryResponse> {
    return this.http.post<GenerateAiSummaryResponse>(`${this.baseUrl(sessionId)}/generate-ai-summary`, {});
  }

  generateNarrativeDraft(sessionId: string): Observable<NarrativeDraft> {
    return this.http.post<NarrativeDraft>(`${this.baseUrl(sessionId)}/generate-narrative-draft`, {});
  }
}