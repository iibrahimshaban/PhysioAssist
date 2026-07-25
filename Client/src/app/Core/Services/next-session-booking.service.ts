import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { GetNextSessionCandidatesRequest, NextSessionBookingContextDto, SessionBookingRoundDto, SlotCandidateDto } from '../../Shared/Models/next-session-booking.model';

@Injectable({
  providedIn: 'root',
})
export class NextSessionBookingService {
  private http = inject(HttpClient);
  private api = `${environment.apiUrl}DoctorPatientForSchedule`;

  getContext(sessionId: string) {
    return this.http.get<NextSessionBookingContextDto>(
      `${this.api}/session/${sessionId}/next-booking-context`,
    );
  }

  getNextSessionCandidates(packageId: string, request: GetNextSessionCandidatesRequest) {
    return this.http.post<SessionBookingRoundDto>(
      `${this.api}/${packageId}/next-session-candidates`,
      request,
    );
  }

  confirmSlot(packageId: string, candidate: SlotCandidateDto) {
    return this.http.post<{ id: string }>(`${this.api}/packages/${packageId}/confirm-slot`, candidate);
  }

  extendPackage(packageId: string) {
    return this.http.post<void>(`${this.api}/${packageId}/extend`, {});
  }
}
