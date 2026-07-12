import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PublicIntakeFormResponse,
  PublicIntakeSubmissionResponse,
  SubmitPreVisitIntakeRequest
} from '../models';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class QrAccessService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}public`;

  getPublicForm(token: string): Observable<PublicIntakeFormResponse> {
    return this.http.get<PublicIntakeFormResponse>(`${this.baseUrl}/intake/${token}`);
  }

  submitPublicIntake(token: string, request: SubmitPreVisitIntakeRequest): Observable<PublicIntakeSubmissionResponse> {
    return this.http.post<PublicIntakeSubmissionResponse>(`${this.baseUrl}/intake/${token}/submit`, request);
  }

  extractTokenFromUrl(url: string): string | null {
    const match = url.match(/\/intake\/([^\/\?]+)/);
    return match ? match[1] : null;
  }

  isTokenExpired(expiresAt: string): boolean {
    return new Date(expiresAt) <= new Date();
  }

  generatePublicUrl(token: string): string {
    const normalizedToken = token?.trim();
    if (!normalizedToken) {
      return `${window.location.origin}/public`;
    }

    const baseUrl = new URL(window.location.href);
    baseUrl.pathname = `/public/intake/${normalizedToken}`;
    return baseUrl.toString();
  }
}
