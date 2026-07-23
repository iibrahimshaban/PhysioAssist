import { Component, input, output } from '@angular/core';
import { PatientSessionPackageSummaryDto } from '../SessionScheduling.model';
import { ButtonModule } from 'primeng/button';
import { SlicePipe } from '@angular/common';
import { ProgressBar } from "primeng/progressbar";
import { Tag } from "primeng/tag";

@Component({
  selector: 'app-session-plan-header',
  imports: [ButtonModule, SlicePipe, ProgressBar, Tag],
  templateUrl: './session-plan-header.component.html',
  styleUrl: './session-plan-header.component.css',
})
export class SessionPlanHeaderComponent {
   summary = input.required<PatientSessionPackageSummaryDto>();
  patientName = input<string>('');
  planDescription = input<string>('');

  scheduledThisWeek = input<number>(0);
  weeklyTargetCount = input<number>(0);
  weekStart = input<string | null>(null);
  weekEnd = input<string | null>(null);

  back = output<void>();
  manualSchedule = output<void>();
}
