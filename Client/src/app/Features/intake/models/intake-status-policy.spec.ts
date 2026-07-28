import { describe, expect, it } from 'vitest';
import { IntakeStatus } from './intake-submission.model';
import {
  canEditIntake,
  getAvailableIntakeActions,
  getIntakeStatusLabel,
  getIntakeStatusPillClass,
} from './intake-status-policy';

describe('intake status policy', () => {
  it.each([
    [IntakeStatus.Pending, 'Pending'],
    [IntakeStatus.Submitted, 'Submitted'],
    [IntakeStatus.InReview, 'In Review'],
    [IntakeStatus.Approved, 'Approved'],
    [IntakeStatus.Rejected, 'Rejected'],
    [IntakeStatus.Converted, 'Converted'],
    [IntakeStatus.Expired, 'Expired'],
  ])('uses the exact label for %s', (status, label) => {
    expect(getIntakeStatusLabel(status)).toBe(label);
    expect(getIntakeStatusPillClass(status)).not.toBe('status-badge-neutral');
  });

  it('allows approval from every reviewable state', () => {
    for (const status of [IntakeStatus.Pending, IntakeStatus.Submitted, IntakeStatus.InReview]) {
      expect(getAvailableIntakeActions(status)).toContainEqual(expect.objectContaining({
        type: 'status',
        status: IntakeStatus.Approved,
        label: 'Approve',
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
