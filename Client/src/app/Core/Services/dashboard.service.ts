import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { DoctorDashboardSummary } from '../../Shared/Models/dashboard.model';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}DoctorDashboard`;

  getSummary() {
    return this.http.get<DoctorDashboardSummary>(`${this.baseUrl}/summary`);
  }
}
