import { IntakeStatus } from './intake-submission.model';

export type IntakeAction = {
  type: 'status' | 'convert';
  status: IntakeStatus;
  /** Translation key under `intake.action.*` — translate at render time. */
  labelKey: string;
  icon: string;
  severity: 'info' | 'warn' | 'success' | 'danger' | 'secondary' | 'contrast';
  /** Translation key for the confirmation message, or '' when none. */
  messageKey: string;
};

/** Single source of truth mapping IntakeStatus → translation key. */
export const INTAKE_STATUS_KEYS: Record<IntakeStatus, string> = {
  [IntakeStatus.Pending]: 'intake.status.pending',
  [IntakeStatus.Submitted]: 'intake.status.submitted',
  [IntakeStatus.InReview]: 'intake.status.inReview',
  [IntakeStatus.Approved]: 'intake.status.approved',
  [IntakeStatus.Rejected]: 'intake.status.rejected',
  [IntakeStatus.Converted]: 'intake.status.converted',
  [IntakeStatus.Expired]: 'intake.status.expired',
};

export const INTAKE_STATUS_UNKNOWN_KEY = 'intake.status.unknown';

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
  labelKey: 'intake.action.approve',
  icon: 'pi pi-check-circle',
  severity: 'success',
  messageKey: 'intake.action.msgApprove'
};

const rejectAction: IntakeAction = {
  type: 'status',
  status: IntakeStatus.Rejected,
  labelKey: 'intake.action.reject',
  icon: 'pi pi-times-circle',
  severity: 'danger',
  messageKey: 'intake.action.msgReject'
};

export function getIntakeStatusKey(status: IntakeStatus | null | undefined): string {
  return (status != null && INTAKE_STATUS_KEYS[status]) || INTAKE_STATUS_UNKNOWN_KEY;
}

export function getIntakeStatusPillClass(status: IntakeStatus): string {
  return statusPillClasses[status] ?? 'status-badge-neutral';
}

export function getAvailableIntakeActions(status: IntakeStatus): IntakeAction[] {
  switch (status) {
    case IntakeStatus.Pending:
    case IntakeStatus.Submitted:
      return [
        { type: 'status', status: IntakeStatus.InReview, labelKey: 'intake.action.markInReview', icon: 'pi pi-eye', severity: 'info', messageKey: 'intake.action.msgMarkInReview' },
        approveAction,
        rejectAction,
      ];
    case IntakeStatus.InReview:
      return [approveAction, rejectAction];
    case IntakeStatus.Approved:
      return [
        { type: 'convert', status: IntakeStatus.Converted, labelKey: 'intake.action.convertToPatient', icon: 'pi pi-user-plus', severity: 'success', messageKey: '' },
        rejectAction,
      ];
    case IntakeStatus.Rejected:
      return [
        { type: 'status', status: IntakeStatus.InReview, labelKey: 'intake.action.reopenReview', icon: 'pi pi-undo', severity: 'info', messageKey: 'intake.action.msgReopenReview' },
        approveAction
      ];
    default:
      return [];
  }
}

export function canEditIntake(status: IntakeStatus | null | undefined): boolean {
  return status === IntakeStatus.Approved;
}
