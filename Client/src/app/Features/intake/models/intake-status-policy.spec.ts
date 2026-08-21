import { describe, expect, it } from 'vitest';
import { IntakeStatus } from './intake-submission.model';
import {
  canEditIntake,
  getAvailableIntakeActions,
  getIntakeStatusKey,
  getIntakeStatusPillClass,
} from './intake-status-policy';

describe('intake status policy', () => {
  it.each([
    [IntakeStatus.Pending, 'intake.status.pending'],
    [IntakeStatus.Submitted, 'intake.status.submitted'],
    [IntakeStatus.InReview, 'intake.status.inReview'],
    [IntakeStatus.Approved, 'intake.status.approved'],
    [IntakeStatus.Rejected, 'intake.status.rejected'],
    [IntakeStatus.Converted, 'intake.status.converted'],
    [IntakeStatus.Expired, 'intake.status.expired'],
  ])('uses the exact translation key for %s', (status, key) => {
    expect(getIntakeStatusKey(status)).toBe(key);
    expect(getIntakeStatusPillClass(status)).not.toBe('status-badge-neutral');
  });

  it('falls back to the unknown key for invalid statuses', () => {
    expect(getIntakeStatusKey(undefined)).toBe('intake.status.unknown');
    expect(getIntakeStatusKey(999 as IntakeStatus)).toBe('intake.status.unknown');
  });

  it('allows approval from every reviewable state', () => {
    for (const status of [IntakeStatus.Pending, IntakeStatus.Submitted, IntakeStatus.InReview]) {
      expect(getAvailableIntakeActions(status)).toContainEqual(expect.objectContaining({
        type: 'status',
        status: IntakeStatus.Approved,
        labelKey: 'intake.action.approve',
      }));
    }
  });

  it('allows conversion only after approval', () => {
    for (const status of Object.values(IntakeStatus).filter((value): value is IntakeStatus => typeof value === 'number')) {
      const hasConvertAction = getAvailableIntakeActions(status).some(action => action.type === 'convert');
      expect(hasConvertAction).toBe(status === IntakeStatus.Approved);
      expect(canEditIntake(status)).toBe(status === IntakeStatus.Approved);
    }
  });

  it('keeps converted and expired submissions terminal', () => {
    expect(getAvailableIntakeActions(IntakeStatus.Converted)).toEqual([]);
    expect(getAvailableIntakeActions(IntakeStatus.Expired)).toEqual([]);
  });
});
