import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { GetNextSessionCandidatesRequest, NextSessionBookingContextDto, SessionBookingRoundDto, SlotCandidateDto } from '../../Shared/Models/next-session-booking.model';
import { SKIP_GLOBAL_LOADING } from '../Interceptors/skip-global-loading.token';

@Injectable({
  providedIn: 'root',
})
export class NextSessionBookingService {
  private http = inject(HttpClient);
  private api = `${environment.apiUrl}DoctorPatientForSchedule`;

  getContext(sessionId: string) {
    return this.http.get<NextSessionBookingContextDto>(
      `${this.api}/session/${sessionId}/next-booking-context`,
      { context: new HttpContext().set(SKIP_GLOBAL_LOADING, true) },
    );
  }

  getNextSessionCandidates(packageId: string, request: GetNextSessionCandidatesRequest) {
    return this.http.post<SessionBookingRoundDto>(
      `${this.api}/${packageId}/next-session-candidates`,
      request,
      { context: new HttpContext().set(SKIP_GLOBAL_LOADING, true) },
    );
  }

  confirmSlot(packageId: string, candidate: SlotCandidateDto) {
    return this.http.post<{ id: string }>(
      `${this.api}/packages/${packageId}/confirm-slot`,
      candidate,
      { context: new HttpContext().set(SKIP_GLOBAL_LOADING, true) },
    );
  }

  extendPackage(packageId: string) {
    return this.http.post<void>(
      `${this.api}/${packageId}/extend`,
      {},
      { context: new HttpContext().set(SKIP_GLOBAL_LOADING, true) },
    );
  }
}
