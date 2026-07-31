import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
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
  CompleteGoogleOnboardingRequest,
  GoogleLoginResponse,
  GoogleLoginRequest,
} from '../../Shared/Models/Auth.Modules';
import { DefaultRoles } from '../const/DefaultRoles';
import { catchError, finalize, map, Observable, of, shareReplay, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiUrl}auth`;

  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private inFlightRefresh$: Observable<AuthResponse> | null = null;
  // ─── Storage keys ──────────────────────────────────────────────────────────

  private readonly TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';

  // ─── State ─────────────────────────────────────────────────────────────────

  currentUser = signal<CurrentUser | null>(null);
  isAuthenticated = computed(() => !!this.currentUser());

  roles = computed(() => this.currentUser()?.roles ?? []);

  isDoctor = computed(() =>
    this.roles().includes(DefaultRoles.SoloDoctor) || this.roles().includes(DefaultRoles.Admin),
  );

  isReceptionist = computed(() => this.roles().includes(DefaultRoles.Receptionist));

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

    if (this.inFlightRefresh$) {
      return this.inFlightRefresh$;
    }

    const request: RefreshTokenRequest = { token, refreshToken };

    this.inFlightRefresh$ = this.http.post<AuthResponse>(`${this.baseUrl}/new-refresh`, request).pipe(
      tap(response => this.handleAuthResponse(response)),
      finalize(() => {
        this.inFlightRefresh$ = null;
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    return this.inFlightRefresh$;
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

  loginWithGoogle(request: GoogleLoginRequest) {
    return this.http.post<GoogleLoginResponse>(`${this.baseUrl}/google`, request);
    // no .pipe(tap(handleAuthResponse)) here — branching happens in the component,
    // since a NeedsOnboarding response has no token to store yet
  }

  completeGoogleOnboarding(request: CompleteGoogleOnboardingRequest) {
    const form = new FormData();
    form.append('onboardingToken', request.onboardingToken);
    form.append('firstName', request.firstName);
    form.append('lastName', request.lastName);
    form.append('clinicName', request.clinicName);
    if (request.profilePhoto) {
      form.append('profilePhoto', request.profilePhoto);
    }
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/google/complete-onboarding`, form)
      .pipe(tap(response => this.handleAuthResponse(response)));
  }

  // ─── Token accessors (used by the auth interceptor) ────────────────────────

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isTokenExpiringSoon(token: string, bufferSeconds = 30): boolean {
    try {
      const decoded = jwtDecode<JwtPayload>(token);
      if (!decoded.exp) return false; // no exp claim — can't check, let it through
      const expiresAtMs = decoded.exp * 1000;
      return expiresAtMs - bufferSeconds * 1000 < Date.now();
    } catch {
      return true; // malformed/undecodable — force the refresh path
    }
  }

  hasPermission(permission: string): boolean {
    return this.currentUser()?.permissions.includes(permission) ?? false;
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  // ─── Internal helpers ──────────────────────────────────────────────────────

  handleAuthResponse(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    this.currentUser.set(this.decodeUser(response.token));
  }

  initializeAuth() {
    const token = localStorage.getItem(this.TOKEN_KEY);
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);

    if (!token || !refreshToken) {
      return of(false);
    }

    let decoded: JwtPayload;
    try {
      decoded = jwtDecode<JwtPayload>(token);
    } catch {
      this.clearStorage();
      return of(false);
    }

    const isExpired = (decoded.exp ?? 0) * 1000 < Date.now();

    if (!isExpired) {
      this.currentUser.set(this.buildUser(decoded));
      return of(true);
    }

    // Access token expired, refresh token present — try to use it now,
    // synchronously as part of app startup, before any guard runs.
    const refresh$ = this.refreshToken();
    if (!refresh$) return of(false);

    return refresh$.pipe(
      map(() => true),
      catchError(() => {
        this.clearStorage();
        this.currentUser.set(null);
        return of(false);
      })
    );
  }

  private decodeUser(token: string): CurrentUser {
    const decoded = jwtDecode<JwtPayload>(token);
    return this.buildUser(decoded);
  }

  private buildUser(decoded: JwtPayload): CurrentUser {
    const roles = decoded['Roles'] ?? [];
    return {
      id: decoded.sub ?? '',
      email: decoded.email ?? '',
      firstName: decoded.given_name ?? '',
      lastName: decoded.family_name ?? '',
      role: roles[0] ?? '',
      roles,
      permissions: decoded['Permissions'] ?? [],
      profilePictureUrl: decoded['profilePictureUrl'] ?? '',
    };
  }

  private clearStorage(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  }
}

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