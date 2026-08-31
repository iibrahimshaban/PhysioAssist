import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SlotCandidateDto } from '../../Shared/Models/InitialReport.models';
import {
  CreateSessionPackageResult,
  SessionBookingRoundDto,
  GetNextSessionCandidatesRequest,
  PatientSessionPackageSummaryDto,
  PatientSchedulingContextDto,
  ConvertPlanToPackageRequest,
  ExtendPackageRequest,
  StopPackageRequest,
} from './SessionScheduling.model';
import { ScheduleSlotDto } from '../../Features/Schedule/schedule.models';

@Injectable({ providedIn: 'root' })
export class ReceptionistSchedulingService {
  private readonly receptionistBaseUrl = `${environment.apiUrl}Receptionist`;
  private readonly packageBaseUrl = `${environment.apiUrl}Package`;
  private readonly http = inject(HttpClient);

  summary = signal<PatientSessionPackageSummaryDto | null>(null);
  currentRound = signal<SessionBookingRoundDto | null>(null);
  isLoading = signal(false);

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
      .post<SessionBookingRoundDto>(
        `${this.receptionistBaseUrl}/packages/${packageId}/next-candidates`,
        body
      )
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
      .post<ScheduleSlotDto>(`${this.receptionistBaseUrl}/packages/${packageId}/confirm-slot`, chosenSlot)
      .pipe(
        tap(() => {
          this.currentRound.set(null);
        }),
      );
  }

  getPackageSummary(packageId: string) {
    return this.http
      .get<PatientSessionPackageSummaryDto>(`${this.receptionistBaseUrl}/packages/${packageId}/summary`)
      .pipe(tap(summary => this.summary.set(summary)));
  }

  getSchedulingContext(patientId: string) {
    return this.http.get<PatientSchedulingContextDto>(
      `${this.receptionistBaseUrl}/patients/${patientId}/scheduling-context`,
    );
  }

  convertPlanToPackage(treatmentPlanId: string, request: ConvertPlanToPackageRequest) {
    return this.http.post<PatientSessionPackageSummaryDto>(
      `${this.receptionistBaseUrl}/treatment-plans/${treatmentPlanId}/convert-to-package`,
      request,
    );
  }

  // NEW — these live on PackageController, not ReceptionistController
  extendPackage(packageId: string, request: ExtendPackageRequest): Observable<PatientSessionPackageSummaryDto> {
    return this.http
      .post<PatientSessionPackageSummaryDto>(`${this.packageBaseUrl}/${packageId}/extend`, request)
      .pipe(tap(summary => this.summary.set(summary)));
  }

  stopPackage(packageId: string, request: StopPackageRequest): Observable<PatientSessionPackageSummaryDto> {
    return this.http
      .post<PatientSessionPackageSummaryDto>(`${this.packageBaseUrl}/${packageId}/stop`, request)
      .pipe(tap(summary => this.summary.set(summary)));
  }
}