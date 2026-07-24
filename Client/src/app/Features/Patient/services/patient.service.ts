import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PatientScheduleOverviewDto } from '../../../Shared/Models/Patient.model';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class PatientService {
  private apiUrl = `${environment.apiUrl}patient`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<any[]>(this.apiUrl);
  }
  getById(id: string) {

    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  create(request: any) {
    return this.http.post<any>(this.apiUrl, request);
  }

  update(id: string, request: any) {
    return this.http.put<any>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  assignPatient(patientId: string, doctorId: string) {
    return this.http.post(`${this.apiUrl}/${patientId}/assign/${doctorId}`, {});
  }

  dischargePatient(patientId: string, doctorId: string) {
    return this.http.put(`${this.apiUrl}/${patientId}/discharge/${doctorId}`, {});
  }

  setPrimaryDoctor(patientId: string, doctorId: string) {
    return this.http.put(`${this.apiUrl}/${patientId}/set-primary/${doctorId}`, {});
  }

  updateStatus(id: string, status: number) {
    return this.http.put(`${this.apiUrl}/${id}/status`, status);
  }

  getWithSlots() {
  return this.http.get<any[]>(`${this.apiUrl}/with-slots`);
}
  getOverview(id: string) {
  return this.http.get<any>(`${this.apiUrl}/${id}/overview`);
}

getFormSchema(schemaId: string) {
  return this.http.get<any>(`https://localhost:7097/api/intake/form-schemas/${schemaId}`);
}

updateOverviewSubmission(patientId: string, body: { formSubmissionData: string }) {
  return this.http.put(`${this.apiUrl}/${patientId}/overview/submission-data`, body);
}

getScheduleOverview(patientId: string): Observable<PatientScheduleOverviewDto> {
  return this.http.get<PatientScheduleOverviewDto>(`${this.apiUrl}/${patientId}/schedule-overview`);
}

}