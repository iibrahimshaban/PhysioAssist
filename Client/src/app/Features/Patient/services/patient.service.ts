import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateDirectIntakeRequest,
  PatientScheduleOverviewDto,
} from '../../../Shared/Models/Patient.model';
import {
  PatientRequest,
  PatientResponse,
  PatientWithNextSlotResponse,
  PatientOverviewResponse,
  UpdateSubmissionDataRequest,
  PatientStatus,
} from '../../../Shared/Models/Patient.model';
import { ConvertIntakeToPatientRequest, CreateFormSchemaRequest, FormSchemaResponse, FormSchemaSummaryResponse, GenerateIntakeQrLinkRequest, IntakeStatus, PreVisitIntakeDetailsResponse, PreVisitIntakeResponse, PublishFormSchemaRequest, UpdateFormSchemaRequest, UpdateIntakeStatusRequest } from '../../intake/models';

@Injectable({
  providedIn: 'root',
})
export class PatientService {
  private intakeUrl = `${environment.apiUrl}intake`;
  private apiUrl = `${environment.apiUrl}patient`;

  constructor(private http: HttpClient) {}

  // getAll(): Observable<PatientResponse[]> {
  //   return this.http.get<PatientResponse[]>(this.apiUrl);
  // }

  getById(id: string): Observable<PatientResponse> {
    return this.http.get<PatientResponse>(`${this.apiUrl}/${id}`);
  }

  create(request: PatientRequest): Observable<PatientResponse> {
    return this.http.post<PatientResponse>(this.apiUrl, request);
  }

  update(id: string, request: PatientRequest): Observable<PatientResponse> {
    return this.http.put<PatientResponse>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  updateStatus(id: string, status: PatientStatus): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/status`, status);
  }

  // ---- Patient <-> Doctor relationships ----

  // assignPatient(patientId: string, doctorId: string): Observable<void> {
  //   return this.http.post<void>(`${this.apiUrl}/${patientId}/assign/${doctorId}`, {});
  // }

  // dischargePatient(patientId: string, doctorId: string): Observable<void> {
  //   return this.http.put<void>(`${this.apiUrl}/${patientId}/discharge/${doctorId}`, {});
  // }

  // setPrimaryDoctor(patientId: string, doctorId: string): Observable<void> {
  //   return this.http.put<void>(`${this.apiUrl}/${patientId}/set-primary/${doctorId}`, {});
  // }

  // ---- Slots / overview ----

  getWithSlots(): Observable<PatientWithNextSlotResponse[]> {
    return this.http.get<PatientWithNextSlotResponse[]>(`${this.apiUrl}/with-slots`);
  }

  getOverview(id: string): Observable<PatientOverviewResponse> {
    return this.http.get<PatientOverviewResponse>(`${this.apiUrl}/${id}/overview`);
  }

  updateOverviewSubmission(
    patientId: string,
    request: UpdateSubmissionDataRequest
  ): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${patientId}/overview/submission-data`, request);
  }

  getScheduleOverview(patientId: string): Observable<PatientScheduleOverviewDto> {
    return this.http.get<PatientScheduleOverviewDto>(`${this.apiUrl}/${patientId}/schedule-overview`);
  }

  // ---- Form schemas ----

  createFormSchema(request: CreateFormSchemaRequest): Observable<FormSchemaResponse> {
    return this.http.post<FormSchemaResponse>(`${this.intakeUrl}/form-schemas`, request);
  }

  updateFormSchema(schemaId: string, request: UpdateFormSchemaRequest): Observable<FormSchemaResponse> {
    return this.http.put<FormSchemaResponse>(`${this.intakeUrl}/form-schemas/${schemaId}`, request);
  }

  publishFormSchema(schemaId: string, request: PublishFormSchemaRequest): Observable<FormSchemaResponse> {
    return this.http.post<FormSchemaResponse>(`${this.intakeUrl}/form-schemas/${schemaId}/publish`, request);
  }

  getFormSchema(schemaId: string): Observable<FormSchemaResponse> {
    return this.http.get<FormSchemaResponse>(`${this.intakeUrl}/form-schemas/${schemaId}`);
  }

  getDefaultFormSchema(): Observable<FormSchemaResponse> {
    return this.http.get<FormSchemaResponse>(`${this.intakeUrl}/form-schemas/default`);
  }

  getAllFormSchemas(): Observable<FormSchemaSummaryResponse[]> {
    return this.http.get<FormSchemaSummaryResponse[]>(`${this.intakeUrl}/form-schemas`);
  }

  generateIntakeQrLink(schemaId: string, request: GenerateIntakeQrLinkRequest): Observable<any> {
    // response DTO for this endpoint not shared — typed as `any`, confirm shape
    return this.http.post<any>(`${this.intakeUrl}/form-schemas/${schemaId}/qr-link`, request);
  }

  // ---- Intake submissions ----

  getSubmissions(status?: IntakeStatus): Observable<PreVisitIntakeResponse[]> {
  let params = new HttpParams();
  if (status !== undefined) {
    params = params.set('status', status.toString());
  }
  return this.http.get<PreVisitIntakeResponse[]>(`${this.intakeUrl}/submissions`, { params });
}

  getSubmissionDetails(id: string): Observable<PreVisitIntakeDetailsResponse> {
    return this.http.get<PreVisitIntakeDetailsResponse>(`${this.intakeUrl}/submissions/${id}`);
  }

  updateIntakeStatus(id: string, request: UpdateIntakeStatusRequest): Observable<PreVisitIntakeResponse> {
    return this.http.patch<PreVisitIntakeResponse>(`${this.intakeUrl}/submissions/${id}/status`, request);
  }

  convertIntakeToPatient(
  intakeId: string,
  request: ConvertIntakeToPatientRequest = {}
): Observable<PreVisitIntakeResponse> {
  return this.http.post<PreVisitIntakeResponse>(
    `${this.intakeUrl}/submissions/${intakeId}/convert-to-patient`,
    request
  );
}

  createDirectIntake(request: CreateDirectIntakeRequest): Observable<PreVisitIntakeResponse> {
    return this.http.post<PreVisitIntakeResponse>(`${this.intakeUrl}/direct`, request);
  }
}