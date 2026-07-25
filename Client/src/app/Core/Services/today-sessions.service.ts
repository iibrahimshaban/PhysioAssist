import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { TodaySessionsOverviewDto } from '../../Shared/Models/today-sessions.model';
import { Observable } from 'rxjs';

interface StartSessionResponse {
  id: string;
  patientId: string;
  doctorId: string;
  scheduleSlotId: string | null;
  summary: string | null;
  status: number;
}

@Injectable({
  providedIn: 'root',
})

export class TodaySessionsService {
  private readonly http = inject(HttpClient);
  private readonly scheduleBaseUrl = `${environment.apiUrl}DoctorPatientForSchedule`;
  private readonly sessionBaseUrl = `${environment.apiUrl}Session`;

  getTodaySessions(): Observable<TodaySessionsOverviewDto> {
    return this.http.get<TodaySessionsOverviewDto>(`${this.scheduleBaseUrl}/today-sessions`);
  }

  startOrResumeSession(patientId: string, scheduleSlotId: string): Observable<StartSessionResponse> {
    return this.http.post<StartSessionResponse>(`${this.sessionBaseUrl}/start`, { patientId, scheduleSlotId });
  }

  markNoShow(slotId: string, countsAsUsed: boolean): Observable<void> {
    return this.http.put<void>(`${this.sessionBaseUrl}/slots/${slotId}/no-show`, { countsAsUsed });
  }
}
