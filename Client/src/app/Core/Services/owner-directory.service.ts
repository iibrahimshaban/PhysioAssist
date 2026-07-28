import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment.development';
import { GuestService } from './guest.service';
import { PatientResponse, GuestResponse, shortId } from '../../Features/Schedule/schedule.models';

const DOCTOR_PATIENTS_BASE = `${environment.apiUrl}DoctorPatientForSchedule`;

export interface OwnerInfo {
  kind: 'patient' | 'guest' | 'unknown';
  name: string;
  phoneNumber?: string | null;
  emailAddress?: string | null;
}

@Injectable({ providedIn: 'root' })
export class OwnerDirectoryService {
  private readonly http = inject(HttpClient);
  private readonly guestService = inject(GuestService);

  private readonly _patientsById = signal<Map<string, PatientResponse>>(new Map());
  private readonly _guestsById = signal<Map<string, GuestResponse>>(new Map());

  readonly patientsById = this._patientsById.asReadonly();
  readonly guestsById = this._guestsById.asReadonly();

  private patientsLoadedForDoctor: string | null = null;
  private patientsLoadingPromise: Promise<void> | null = null;
  private readonly pendingGuestIds = new Set<string>();

  /**
   * Call whenever the visible appointment set changes. Loads the doctor's
   * full patient roster once per doctor, and batch-fetches only the guests
   * referenced here that aren't already cached — never one call per guest.
   */
  async ensureLoaded(
    doctorId: string,
    appointments: { patientId: string | null; guestId: string | null }[]
  ): Promise<void> {
    const patientsPromise = this.ensurePatientsLoaded(doctorId);

    const missingGuestIds = [...new Set(
      appointments
        .map(a => a.guestId)
        .filter((id): id is string =>
          !!id && !this._guestsById().has(id) && !this.pendingGuestIds.has(id))
    )];

    let guestsPromise: Promise<void> = Promise.resolve();
    if (missingGuestIds.length > 0) {
      missingGuestIds.forEach(id => this.pendingGuestIds.add(id));
      guestsPromise = this.guestService.getGuestsByIds(missingGuestIds)
        .then(guests => {
          const map = new Map(this._guestsById());
          guests.forEach(g => map.set(g.id, g));
          this._guestsById.set(map);
        })
        .finally(() => missingGuestIds.forEach(id => this.pendingGuestIds.delete(id)));
    }

    await Promise.all([patientsPromise, guestsPromise]);
  }

  private ensurePatientsLoaded(doctorId: string): Promise<void> {
    if (this.patientsLoadedForDoctor === doctorId) return Promise.resolve();
    if (this.patientsLoadingPromise) return this.patientsLoadingPromise;

    this.patientsLoadingPromise = firstValueFrom(
      this.http.get<PatientResponse[]>(DOCTOR_PATIENTS_BASE)
    ).then(list => {
      this._patientsById.set(new Map(list.map(p => [p.id, p])));
      this.patientsLoadedForDoctor = doctorId;
    }).finally(() => {
      this.patientsLoadingPromise = null;
    });

    return this.patientsLoadingPromise;
  }

  /** Synchronous — reads whatever's cached right now. Falls back to a short-id label while a fetch is still in flight. */
  resolveOwner(appointment: { patientId: string | null; guestId: string | null }): OwnerInfo {
    if (appointment.guestId) {
      const guest = this._guestsById().get(appointment.guestId);
      return guest
        ? { kind: 'guest', name: guest.fullName, phoneNumber: guest.phoneNumber }
        : { kind: 'guest', name: `Guest ${shortId(appointment.guestId)}` };
    }
    if (appointment.patientId) {
      const patient = this._patientsById().get(appointment.patientId);
      return patient
        ? { kind: 'patient', name: patient.fullName, phoneNumber: patient.phoneNumber, emailAddress: patient.emailAddress }
        : { kind: 'patient', name: `Patient ${shortId(appointment.patientId)}` };
    }
    return { kind: 'unknown', name: 'Unknown' };
  }
}