import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateWorkingScheduleRequest,
  UpdateWorkingScheduleDaysRequest,
  WorkingScheduleDto,
} from '../../Features/WorkingSchedule/WorkingSchedule.models';
import { SKIP_ERROR_SNACKBAR } from '../Interceptors/skip-error-interceptor.token';

@Injectable({ providedIn: 'root' })
export class WorkingScheduleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}WorkingSchedules`;

  /**
   * The doctor is resolved server-side from the auth token — no id is ever sent.
   * A 404 (no schedule created yet) is a normal state here, not an error, so it
   * resolves to `null` instead of surfacing the global error snackbar.
   */
  getActiveSchedule(): Observable<WorkingScheduleDto | null> {
    return this.http
      .get<WorkingScheduleDto>(`${this.baseUrl}/doctor`, {
        context: new HttpContext().set(SKIP_ERROR_SNACKBAR, true),
      })
      .pipe(catchError(() => of(null)));
  }

  create(request: CreateWorkingScheduleRequest): Observable<WorkingScheduleDto> {
    return this.http.post<WorkingScheduleDto>(this.baseUrl, request);
  }

  updateDays(id: string, request: UpdateWorkingScheduleDaysRequest): Observable<WorkingScheduleDto> {
    return this.http.put<WorkingScheduleDto>(`${this.baseUrl}/${id}/days`, request);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/deactivate`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
