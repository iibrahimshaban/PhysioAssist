import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { DoctorSchedulingPreference, UpdateDoctorSchedulingPreferenceRequest } from '../../Shared/Models/Doctorschedulingpreference.model';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DoctorSchedulingPreferenceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}DoctorSchedulingPreferences`;
 
  get(): Observable<DoctorSchedulingPreference> {
    return this.http.get<DoctorSchedulingPreference>(this.baseUrl);
  }
 
  update(request: UpdateDoctorSchedulingPreferenceRequest): Observable<DoctorSchedulingPreference> {
    return this.http.put<DoctorSchedulingPreference>(this.baseUrl, request);
  }
}
