import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Receptionist,
  PermissionInfo,
  CreateReceptionistRequest,
  UpdateReceptionistRequest,
} from '../../Shared/Models/staff.model';

@Injectable({ providedIn: 'root' })
export class StaffService {
  private readonly baseUrl = `${environment.apiUrl}Receptionist`;
  private readonly http = inject(HttpClient);

  receptionists = signal<Receptionist[]>([]);
  availablePermissions = signal<PermissionInfo[]>([]);
  isLoading = signal(false);

  loadReceptionists(): void {
    this.isLoading.set(true);
    this.http.get<Receptionist[]>(this.baseUrl).subscribe({
      next: list => {
        this.receptionists.set(list);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  loadPermissions(): void {
    this.http
      .get<PermissionInfo[]>(`${this.baseUrl}/permissions`)
      .subscribe(perms => this.availablePermissions.set(perms));
  }

  create(request: CreateReceptionistRequest) {
    return this.http.post<Receptionist>(this.baseUrl, request).pipe(
      tap(created => this.receptionists.update(list => [...list, created])),
    );
  }

  update(id: string, request: UpdateReceptionistRequest) {
    return this.http.put<Receptionist>(`${this.baseUrl}/${id}`, request).pipe(
      tap(updated =>
        this.receptionists.update(list => list.map(r => (r.id === id ? updated : r))),
      ),
    );
  }

  toggleDisabled(id: string) {
    return this.http.patch<void>(`${this.baseUrl}/${id}/toggle-disabled`, {}).pipe(
      tap(() =>
        this.receptionists.update(list =>
          list.map(r => (r.id === id ? { ...r, isActive: !r.isActive } : r)),
        ),
      ),
    );
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
      tap(() => this.receptionists.update(list => list.filter(r => r.id !== id))),
    );
  }
}