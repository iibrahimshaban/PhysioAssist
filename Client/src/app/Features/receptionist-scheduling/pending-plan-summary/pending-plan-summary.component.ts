import { Component, input } from '@angular/core';
import { DaysOfWeekFlags, PreferredTimeOfDay, SchedulingPriority } from '../../../Shared/Models/InitialReport.models';
import { PendingTreatmentPlanDto } from '../SessionScheduling.model';

@Component({
  selector: 'app-pending-plan-summary',
  imports: [],
  templateUrl: './pending-plan-summary.component.html',
  styleUrl: './pending-plan-summary.component.css',
})
export class PendingPlanSummaryComponent {
  plan = input<PendingTreatmentPlanDto | null>(null);
 
  protected preferredTimeOfDayLabel(value: PreferredTimeOfDay): string {
    return {
      [PreferredTimeOfDay.Unspecified]: 'Any time',
      [PreferredTimeOfDay.Morning]: 'Morning',
      [PreferredTimeOfDay.Afternoon]: 'Afternoon',
      [PreferredTimeOfDay.Evening]: 'Evening',
    }[value];
  }
 
  protected preferredDaysLabel(flags: DaysOfWeekFlags): string {
    if (flags === DaysOfWeekFlags.None) return 'Any day';
 
    const names: [DaysOfWeekFlags, string][] = [
      [DaysOfWeekFlags.Sunday, 'Sun'],
      [DaysOfWeekFlags.Monday, 'Mon'],
      [DaysOfWeekFlags.Tuesday, 'Tue'],
      [DaysOfWeekFlags.Wednesday, 'Wed'],
      [DaysOfWeekFlags.Thursday, 'Thu'],
      [DaysOfWeekFlags.Friday, 'Fri'],
      [DaysOfWeekFlags.Saturday, 'Sat'],
    ];
 
    return names
      .filter(([flag]) => (flags & flag) === flag)
      .map(([, label]) => label)
      .join(', ');
  }
 
  protected priorityLabel(value: SchedulingPriority): string {
    return {
      [SchedulingPriority.Normal]: 'Normal',
      [SchedulingPriority.Low]: 'Low',
      [SchedulingPriority.High]: 'High',
      [SchedulingPriority.Urgent]: 'Urgent',
    }[value];
  }
}
