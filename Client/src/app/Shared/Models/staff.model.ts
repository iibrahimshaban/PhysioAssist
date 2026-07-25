// Shared/Models/staff.model.ts

export enum ReceptionistShiftType {
  Morning = 1,
  Evening = 2,
  FullDay = 4,
}

export const SHIFT_DEFAULTS: Record<ReceptionistShiftType, { from: string; to: string }> = {
  [ReceptionistShiftType.Morning]: { from: '08:00', to: '16:00' },
  [ReceptionistShiftType.Evening]: { from: '14:00', to: '22:00' },
  [ReceptionistShiftType.FullDay]: { from: '08:00', to: '22:00' },
};

export interface PermissionInfo {
  value: string;
  title: string;
  description: string;
}

export interface Receptionist {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  isActive: boolean;
  shift: ReceptionistShiftType;
  from: string | null;  // "HH:mm:ss"
  to: string | null;    // "HH:mm:ss"
  managingDoctorId: string;
  permissions: string[];
  profilePictureUrl: string | null;
}

export interface CreateReceptionistRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  password: string;
  shift: ReceptionistShiftType;
  from: string | null;  // "HH:mm:ss"
  to: string | null;    // "HH:mm:ss"
  permissions: string[];
}

export interface UpdateReceptionistRequest {
  firstName: string;
  lastName: string;
  phone: string;
  shift: ReceptionistShiftType;
  from: string | null;  // "HH:mm:ss"
  to: string | null;    // "HH:mm:ss"
  permissions: string[];
  newPassword?: string | null;
}