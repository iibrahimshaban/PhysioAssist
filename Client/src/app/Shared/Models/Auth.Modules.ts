// ─── Current user (decoded from JWT) ─────────────────────────────────────────

export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;       // keep for backward compatibility if used elsewhere
  roles: string[];     // new
  permissions: string[];
  profilePictureUrl: string;
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

export interface GoogleLoginRequest {
  idToken: string;
}

export interface GoogleOnboardingRequiredResponse {
  onboardingToken: string;
  email: string;
  suggestedFirstName: string;
  suggestedLastName: string;
}

export type GoogleLoginResponse = AuthResponse | GoogleOnboardingRequiredResponse;

export interface CompleteGoogleOnboardingRequest {
  onboardingToken: string;
  firstName: string;
  lastName: string;
  clinicName: string;
  profilePhoto?: File;
}

// Type guard used by the login component to branch on the response shape
export function requiresOnboarding(res: GoogleLoginResponse): res is GoogleOnboardingRequiredResponse {
  return 'onboardingToken' in res;
}