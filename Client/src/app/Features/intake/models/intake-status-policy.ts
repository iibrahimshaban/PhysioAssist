import { IntakeStatus } from './intake-submission.model';

export type IntakeAction = {
  type: 'status' | 'convert';
  status: IntakeStatus;
  label: string;
  icon: string;
  severity: 'info' | 'warn' | 'success' | 'danger' | 'secondary' | 'contrast';
  message: string;
};

const statusLabels: Record<IntakeStatus, string> = {
  [IntakeStatus.Pending]: 'Pending',
  [IntakeStatus.Submitted]: 'Submitted',
  [IntakeStatus.InReview]: 'In Review',
  [IntakeStatus.Approved]: 'Approved',
  [IntakeStatus.Rejected]: 'Rejected',
  [IntakeStatus.Converted]: 'Converted',
  [IntakeStatus.Expired]: 'Expired',
};

const statusPillClasses: Record<IntakeStatus, string> = {
  [IntakeStatus.Pending]: 'status-badge-warning',
  [IntakeStatus.Submitted]: 'status-badge-warning',
  [IntakeStatus.InReview]: 'status-badge-warning',
  [IntakeStatus.Approved]: 'status-badge-success',
  [IntakeStatus.Rejected]: 'status-badge-danger',
  [IntakeStatus.Converted]: 'status-badge-success',
  [IntakeStatus.Expired]: 'status-badge-danger',
};

const approveAction: IntakeAction = {
  type: 'status',
  status: IntakeStatus.Approved,
  label: 'Approve',
  icon: 'pi pi-check-circle',
  severity: 'success',
  message: 'Approve this submission for patient conversion?'
};

const rejectAction: IntakeAction = {
  type: 'status',
  status: IntakeStatus.Rejected,
  label: 'Reject',
  icon: 'pi pi-times-circle',
  severity: 'danger',
  message: 'Reject this submission? This will mark the intake as rejected.'
};

export function getIntakeStatusLabel(status: IntakeStatus): string {
  return statusLabels[status] ?? 'Unknown';
}

export function getIntakeStatusPillClass(status: IntakeStatus): string {
  return statusPillClasses[status] ?? 'status-badge-neutral';
}

export function getAvailableIntakeActions(status: IntakeStatus): IntakeAction[] {
  switch (status) {
    case IntakeStatus.Pending:
    case IntakeStatus.Submitted:
      return [
        { type: 'status', status: IntakeStatus.InReview, label: 'Mark In Review', icon: 'pi pi-eye', severity: 'info', message: 'Mark this submission as in review?' },
        approveAction,
        rejectAction,
      ];
    case IntakeStatus.InReview:
      return [approveAction, rejectAction];
    case IntakeStatus.Approved:
      return [
        { type: 'convert', status: IntakeStatus.Converted, label: 'Convert to Patient', icon: 'pi pi-user-plus', severity: 'success', message: '' },
        rejectAction,
      ];
    case IntakeStatus.Rejected:
      return [
        { type: 'status', status: IntakeStatus.InReview, label: 'Re-open Review', icon: 'pi pi-undo', severity: 'info', message: 'Re-open this submission for review?' },
        approveAction
      ];
    default:
      return [];
  }
}

export function canEditIntake(status: IntakeStatus | null | undefined): boolean {
  return status === IntakeStatus.Approved;
}
