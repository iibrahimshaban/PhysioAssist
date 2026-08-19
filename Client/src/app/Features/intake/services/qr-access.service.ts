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
    return this.http.get<PublicIntakeFormResponse>(`${this.baseUrl}/intake`, {
      params: { token }
    });
  }

  submitPublicIntake(token: string, request: SubmitPreVisitIntakeRequest): Observable<PublicIntakeSubmissionResponse> {
    return this.http.post<PublicIntakeSubmissionResponse>(`${this.baseUrl}/intake/submit`, request, {
      params: { token }
    });
  }

  checkPatientEmail(email: string): Observable<{ isRegistered: boolean }> {
    const encoded = encodeURIComponent(email.trim());
    return this.http.get<{ isRegistered: boolean }>(
      `${this.baseUrl}/intake/check-email?email=${encoded}`
    );
  }

  extractTokenFromUrl(url: string): string | null {
    try {
      const parsed = new URL(url);
      return parsed.searchParams.get('token');
    } catch {
      return null;
    }
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
    baseUrl.pathname = `/public/intake`;
    baseUrl.searchParams.set('token', normalizedToken);
    return baseUrl.toString();
  }
}
