export interface DoctorSchedulingPreference {
  id: string;
  doctorId: string;
  maxShortfallToleranceMinutes: number;
  maxDaysOutForExactMatch: number;
  allowShorterSlots: boolean;
}
 
export type UpdateDoctorSchedulingPreferenceRequest = Omit<
  DoctorSchedulingPreference,
  'id' | 'doctorId'
>;