import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
  ConfirmEmailRequest,
  ResendConfirmEmailRequest,
  RefreshTokenRequest,
  ForgetPasswordRequest,
  ResetPasswordRequest,
  VerifyResetOtpRequest,
} from '../../Shared/Models/Auth.Modules';


@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiUrl}auth`;

  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // ─── Storage keys ──────────────────────────────────────────────────────────

  private readonly TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';

  // ─── State ─────────────────────────────────────────────────────────────────

  currentUser = signal<CurrentUser | null>(null);
  isAuthenticated = computed(() => !!this.currentUser());

  constructor() {
    this.loadUserFromStorage();
  }

  // ─── Endpoints ─────────────────────────────────────────────────────────────

  login(request: LoginRequest) {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/login`, request)
      .pipe(tap(response => this.handleAuthResponse(response)));
  }

  /** Builds FormData internally — controller uses [FromForm] */
  register(request: RegisterRequest) {
    const form = new FormData();
    form.append('email', request.email);
    form.append('firstName', request.firstName);
    form.append('lastName', request.lastName);
    form.append('password', request.password);
    form.append('clinicName', request.clinicName);
    if (request.profilePhoto) {
      form.append('profilePhoto', request.profilePhoto);
    }
    return this.http.post<void>(`${this.baseUrl}/registration`, form);
  }

  confirmEmail(request: ConfirmEmailRequest) {
    return this.http.post<void>(`${this.baseUrl}/confirm-email`, request);
  }

  resendConfirmationEmail(request: ResendConfirmEmailRequest) {
    return this.http.post<void>(`${this.baseUrl}/resend-confirmation-email`, request);
  }

  /** Called by the auth interceptor and manually when needed. Returns null if no tokens exist. */
  refreshToken() {
    const token = localStorage.getItem(this.TOKEN_KEY);
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);

    if (!token || !refreshToken) return null;

    const request: RefreshTokenRequest = { token, refreshToken };

    return this.http
      .post<AuthResponse>(`${this.baseUrl}/new-refresh`, request)
      .pipe(tap(response => this.handleAuthResponse(response)));
  }

  forgetPassword(request: ForgetPasswordRequest) {
    return this.http.post<void>(`${this.baseUrl}/forget-passowrd`, request);
  }

  resetPassword(request: ResetPasswordRequest) {
    return this.http.post<void>(`${this.baseUrl}/reset-password`, request);
  }

  logout() {
    const token = localStorage.getItem(this.TOKEN_KEY);
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);

    if (token && refreshToken) {
      const request: RefreshTokenRequest = { token, refreshToken };
      // fire-and-forget — client clears state regardless of server response
      this.http.post(`${this.baseUrl}/revoke-refresh-token`, request).subscribe();
    }

    this.clearStorage();
    this.currentUser.set(null);
    this.router.navigateByUrl('/auth/login');
  }

  verifyResetOtp(request: VerifyResetOtpRequest) {
    return this.http.post<void>(`${this.baseUrl}/verify-reset-otp`, request);
  }

  saveResetOtp(otp: string): void {
    sessionStorage.setItem('reset_otp', otp);
  }
  
  getResetOtp(): string | null {
    return sessionStorage.getItem('reset_otp');
  }
  
  clearResetOtp(): void {
    sessionStorage.removeItem('reset_otp');
  }
 
  // ─── Token accessors (used by the auth interceptor) ────────────────────────

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  hasPermission(permission: string): boolean {
    return this.currentUser()?.permissions.includes(permission) ?? false;
  }

  // ─── Internal helpers ──────────────────────────────────────────────────────

  handleAuthResponse(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    this.currentUser.set(this.decodeUser(response.token));
  }

  private loadUserFromStorage(): void {
    const token = localStorage.getItem(this.TOKEN_KEY);
    if (!token) return;

    try {
      const decoded = jwtDecode<JwtPayload>(token);
      const isExpired = (decoded.exp ?? 0) * 1000 < Date.now();

      if (isExpired || !isExpired) {
        this.currentUser.set(this.buildUser(decoded));
      }
    } catch {
      this.clearStorage();
    }
  }

  private decodeUser(token: string): CurrentUser {
    const decoded = jwtDecode<JwtPayload>(token);
    return this.buildUser(decoded);
  }

  private buildUser(decoded: JwtPayload): CurrentUser {
    return {
      id: decoded.sub ?? '',
      email: decoded.email ?? '',
      firstName: decoded.given_name ?? '',
      lastName: decoded.family_name ?? '',
      role: decoded['Roles']?.[0] ?? '',    
      permissions: decoded['Permissions'] ?? [], 
      profilePictureUrl: decoded['profilePictureUrl'] ?? '',
    };
  }
 
  

  private clearStorage(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  }
}

// ─── JWT payload shape ────────────────────────────────────────────────────────
// Extend as you add more claims to GenerateToken()

interface JwtPayload {
  sub?: string;
  email?: string;
  given_name?: string;
  family_name?: string;
  exp?: number;
  profilePictureUrl?: string;
  Roles?: string[];
  Permissions?: string[];
}


