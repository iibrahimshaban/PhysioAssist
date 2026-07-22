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

  /**
   * Streams the answer token-by-token from GET /api/ask/stream (SSE).
   *
   * Deliberately built on `fetch` + `ReadableStream` instead of the
   * browser's `EventSource` API: EventSource cannot send custom headers,
   * so an Authorization header from your HttpClient interceptor would
   * silently be dropped. fetch has no such restriction.
   *
   * Usage: `for await (const delta of this.aiChat.streamAsk(id, q, signal)) { ... }`
   */

  async *streamAsk(conversationId: string,question: string, signal?: AbortSignal,): AsyncGenerator<string> {
    const url = new URL(`${this.baseUrl}ask/stream`);
    url.searchParams.set('conversationId', conversationId);
    url.searchParams.set('question', question);

    const response = await fetch(url.toString(), {
      method: 'GET',
      headers: { Accept: 'text/event-stream' },
      // If your API needs auth, add it here to match whatever your
      // HttpClient interceptor does today, e.g.:
      // headers: { Accept: 'text/event-stream', Authorization: `Bearer ${token}` },
      signal,
    });

    if (!response.ok || !response.body) {
      throw new Error(`Stream request failed with status ${response.status}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
      const { value, done } = await reader.read();
      if (done) return;

      buffer += decoder.decode(value, { stream: true });

      // SSE events are separated by a blank line.
      const rawEvents = buffer.split('\n\n');
      buffer = rawEvents.pop() ?? ''; // last chunk may be incomplete, keep it for next read

      for (const rawEvent of rawEvents) {
        let eventName = 'message';
        let data = '';

        for (const line of rawEvent.split('\n')) {
          if (line.startsWith('event:')) eventName = line.slice(6).trim();
          else if (line.startsWith('data:')) data += line.slice(5).trim();
        }

        if (eventName === 'done') return;
        if (!data) continue;

        try {
          const parsed = JSON.parse(data) as { delta?: string };
          if (parsed.delta)
            yield parsed.delta;
        } catch {
          // malformed chunk, skip it rather than blow up the whole stream
        }
      }
    }
  }
}
