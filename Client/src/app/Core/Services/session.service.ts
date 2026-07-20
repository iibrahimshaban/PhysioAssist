import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { SessionDetailsResponse } from '../../Shared/Models/session-details-response';
import { Suggestion } from '../../Shared/Models/suggestion';

@Injectable({
  providedIn: 'root',
})
export class SessionService {
  private http = inject(HttpClient);
  private api = `${environment.apiUrl}Session`;

  create(request: object) {
    return this.http.post(this.api, request);
  }

  start(id: string) {
    return this.http.put(`${this.api}/${id}/start`, {});
  }

  getById(id: string) {
    return this.http.get(`${this.api}/${id}`);
  }

  getDetails(id: string) {
    return this.http.get<SessionDetailsResponse>(`${this.api}/${id}/details`);
  }

  uploadAudioTranscription(sessionId: string, audioFile: Blob, durationSeconds: number) {
    const formData = new FormData();

    formData.append('audioFile', audioFile, 'recording.webm');
    formData.append('languageHint', 'en');
    formData.append('prompt', '');
    formData.append('durationSeconds', durationSeconds.toString());

    return this.http.post(`${this.api}/${sessionId}/transcription/audio`, formData, {
      responseType: 'text',
    });
  }

  uploadAttachments(sessionId: string, files: File[]) {
    const formData = new FormData();

    for (const file of files) {
      formData.append('Files', file);
    }

    return this.http.post<void>(`${this.api}/${sessionId}/attachments`, formData);
  }

  completeSession(sessionId: string, editedTranscript: string, files: File[]) {
    const formData = new FormData();

    formData.append('EditedTranscript', editedTranscript);

    for (const file of files) {
      formData.append('Files', file);
    }

    return this.http.put<void>(`${this.api}/${sessionId}/complete`, formData);
  }

  saveDraft(sessionId: string, editedTranscript: string, files: File[]) {
    const formData = new FormData();

    formData.append('EditedTranscript', editedTranscript);

    for (const file of files) {
      formData.append('Files', file);
    }

    return this.http.put<void>(`${this.api}/${sessionId}/draft`, formData);
  }

  deleteAttachment(attachmentId: string) {
    return this.http.delete<void>(`${this.api}/attachments/${attachmentId}`);
  }

  getSuggestions(prefix: string, limit: number = 5) {
    return this.http.get<Suggestion[]>(`${environment.apiUrl}AutoComplete/suggest`, {
      params: {
        prefix,
        limit,
      },
    });
  }
}
