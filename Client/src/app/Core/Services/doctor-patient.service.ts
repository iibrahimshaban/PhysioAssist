import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { PatientOption, PatientResponse } from '../../Features/Schedule/schedule.models';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_SNACKBAR } from '../Interceptors/skip-error-interceptor.token';

const DOCTOR_PATIENTS_BASE = `${environment.apiUrl}DoctorPatientForSchedule`;

@Injectable({ providedIn: 'root' })
export class DoctorPatientService {
  private readonly http = inject(HttpClient);

  // doctorId is only used here to key the cache at the call site —
  // the backend reads the real doctor id from the auth token, not this param.
  async getPatientsForDoctor(_doctorId: string): Promise<PatientOption[]> {
    const list = await firstValueFrom(
      this.http.get<PatientResponse[]>(DOCTOR_PATIENTS_BASE, {
        context: new HttpContext().set(SKIP_ERROR_SNACKBAR, true),
      })
    );
    return list.map(p => ({ id: p.id, name: p.fullName, isGuest: false }));
  }
}