import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SlotCandidateDto } from '../../Shared/Models/InitialReport.models';
import {
  ReceptionistCreateSessionPackageRequest,
  CreateSessionPackageResult,
  SessionBookingRoundDto,
  GetNextSessionCandidatesRequest,
  PatientSessionPackageSummaryDto,
  PatientSchedulingContextDto,
  ConvertPlanToPackageRequest,
} from './SessionScheduling.model';
import { ScheduleSlotDto } from '../../Features/Schedule/schedule.models';

@Injectable({ providedIn: 'root' })
export class ReceptionistSchedulingService {
  private readonly baseUrl = `${environment.apiUrl}Receptionist`;
  private readonly http = inject(HttpClient);

  summary = signal<PatientSessionPackageSummaryDto | null>(null);
  currentRound = signal<SessionBookingRoundDto | null>(null);
  isLoading = signal(false);

  createPackage(request: ReceptionistCreateSessionPackageRequest) {
    return this.http.post<CreateSessionPackageResult>(`${this.baseUrl}/packages`, request);
  }

  loadNextSessionCandidates(
    packageId: string,
    patientFreeTimeOverride?: string | null,
    persistFreeTimeOverride = false,
  ): void {
    this.isLoading.set(true);
    const body: GetNextSessionCandidatesRequest = {
      patientFreeTimeOverride: patientFreeTimeOverride ?? null,
      persistFreeTimeOverride,
    };
    this.http
      .post<SessionBookingRoundDto>(`${this.baseUrl}/packages/${packageId}/next-candidates`, body)
      .subscribe({
        next: round => {
          this.currentRound.set(round);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false),
      });
  }

  confirmSlot(packageId: string, chosenSlot: SlotCandidateDto) {
    return this.http
      .post<ScheduleSlotDto>(`${this.baseUrl}/packages/${packageId}/confirm-slot`, chosenSlot)
      .pipe(
        tap(() => {
          this.currentRound.set(null);
        }),
      );
  }
  getPackageSummary(packageId: string) {
  return this.http
    .get<PatientSessionPackageSummaryDto>(`${this.baseUrl}/packages/${packageId}/summary`)
    .pipe(tap(summary => this.summary.set(summary)));
}

  getSchedulingContext(patientId: string) {
    return this.http.get<PatientSchedulingContextDto>(
      `${this.baseUrl}/patients/${patientId}/scheduling-context`,
    );
  }
 
  convertPlanToPackage(treatmentPlanId: string, request: ConvertPlanToPackageRequest) {
    return this.http.post<PatientSessionPackageSummaryDto>(
      `${this.baseUrl}/treatment-plans/${treatmentPlanId}/convert-to-package`,
      request,
    );
  }
}
