import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AiChatResponse, AiClearResponse } from '../../Shared/Models/chat.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AiChatService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}`;

  ask(conversationId: string, question: string): Observable<AiChatResponse> {
    const params = new HttpParams()
      .set('conversationId', conversationId)
      .set('question', question);

    return this.http.get<AiChatResponse>(`${this.baseUrl}ask`, { params });
  }

  /** Tells the backend to drop its in-memory ChatHistory for this id — call this whenever a session is deleted client-side, or that memory just sits there until the process restarts. */
  clear(conversationId: string): Observable<AiClearResponse>
  {
    const params = new HttpParams().set('conversationId', conversationId);

    return this.http.get<AiClearResponse>(`${this.baseUrl}clear`, { params });
  }
}
