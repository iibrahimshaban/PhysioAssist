export interface UserProfile {
  email: string;
  firstName: string;
  lastName: string;
  userName: string;
  phoneNumber?: string | null;
  profilePictureUrl?: string | null;
  title?: string | null;
  clinicName?: string | null;
  clinicAddress?: string | null;
  about?: string | null;
  yearsOfExperience?: number | null;
  profileCompletionPercentage: number;
}

export interface UpdateProfileRequest {
  userName: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  title?: string | null;
  clinicName?: string | null;
  clinicAddress?: string | null;
  about?: string | null;
  yearsOfExperience?: number | null;
  profilePhoto?: File | null;
  removeProfilePhoto: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}