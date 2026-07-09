// account.service.ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment'; // adjust path
import { ChangePasswordRequest, UpdateProfileRequest, UserProfile } from '../../Shared/Models/account.model';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}Account`; // adjust to your baseUrl convention

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(this.baseUrl);
  }

  updateProfile(request: UpdateProfileRequest): Observable<void> {
    const formData = new FormData();

    formData.append('userName', request.userName);
    formData.append('firstName', request.firstName);
    formData.append('lastName', request.lastName);
    formData.append('removeProfilePhoto', String(request.removeProfilePhoto));

    if (request.phoneNumber) formData.append('phoneNumber', request.phoneNumber);
    if (request.title) formData.append('title', request.title);
    if (request.clinicName) formData.append('clinicName', request.clinicName);
    if (request.clinicAddress) formData.append('clinicAddress', request.clinicAddress);
    if (request.about) formData.append('about', request.about);
    if (request.yearsOfExperience != null) {
      formData.append('yearsOfExperience', String(request.yearsOfExperience));
    }
    if (request.profilePhoto) formData.append('profilePhoto', request.profilePhoto);

    return this.http.put<void>(this.baseUrl, formData);
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/change-password`, request);
  }
}