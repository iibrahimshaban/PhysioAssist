// ─── Current user (decoded from JWT) ─────────────────────────────────────────

export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  profilePictureUrl: string;
  permissions: string[]; // decoded from the `permissions` JWT claim
}

// ─── Requests ────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
}

/** AuthService builds the FormData — controller uses [FromForm] */
export interface RegisterRequest {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  clinicName: string;
  profilePhoto?: File;
}

export interface ConfirmEmailRequest {
  email: string;
  code: string;
}

export interface ResendConfirmEmailRequest {
  email: string;
}

export interface RefreshTokenRequest {
  token: string;
  refreshToken: string;
}

export interface ForgetPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  otp: string;
  newPassword: string;
}

// ─── Responses ───────────────────────────────────────────────────────────────

export interface AuthResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  userName: string;
  token: string;
  expiresIn: number;
  refreshToken: string;
  refreshTokenExpiryDate: string;
  profilePictureUrl: string | null;
}
export interface VerifyResetOtpRequest {
  email: string;
  otp: string;
}