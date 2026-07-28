export interface PendingIntakePreview {
  submissionId: string;
  patientFullName: string;
  submittedAt: string;
  painRegionsCount: number;
}

export interface DoctorDashboardSummary {
  doctorFirstName: string;
  pendingIntakesCount: number;
  upcomingSessionsTodayCount: number;
  pendingIntakes: PendingIntakePreview[];
}