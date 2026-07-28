import { Component, input } from '@angular/core';
import { PendingTreatmentPlanDto } from '../SessionScheduling.model';
import { SchedulingPriority } from '../../../Shared/Models/InitialReport.models';

@Component({
  selector: 'app-pending-plan-summary',
  imports: [],
  templateUrl: './pending-plan-summary.component.html',
  styleUrl: './pending-plan-summary.component.css',
})
export class PendingPlanSummaryComponent {
  plan = input<PendingTreatmentPlanDto | null>(null);
 
  protected priorityLabel(value: SchedulingPriority): string {
    return {
      [SchedulingPriority.Normal]: 'Normal',
      [SchedulingPriority.Low]: 'Low',
      [SchedulingPriority.High]: 'High',
      [SchedulingPriority.Urgent]: 'Urgent',
    }[value];
  }
}
