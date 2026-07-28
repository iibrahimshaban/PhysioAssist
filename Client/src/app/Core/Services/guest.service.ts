import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment.development';
import { CreateGuestRequest, GuestResponse } from '../../Features/Schedule/schedule.models';

const GUESTS_BASE = `${environment.apiUrl}guests`;

@Injectable({ providedIn: 'root' })
export class GuestService {
  private readonly http = inject(HttpClient);

  async createGuest(request: CreateGuestRequest): Promise<GuestResponse> {
    return firstValueFrom(this.http.post<GuestResponse>(GUESTS_BASE, request));
  }

  async getGuestsByIds(ids: string[]): Promise<GuestResponse[]> {
    if (ids.length === 0) return [];
    let params = new HttpParams();
    ids.forEach(id => (params = params.append('ids', id)));
    return firstValueFrom(this.http.get<GuestResponse[]>(GUESTS_BASE, { params }));
  }
}