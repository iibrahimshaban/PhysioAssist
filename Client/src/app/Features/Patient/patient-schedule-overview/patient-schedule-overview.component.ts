import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { Tag } from 'primeng/tag';
import { PatientScheduleOverviewDto, SlotStatus } from '../../../Shared/Models/Patient.model';

@Component({
  selector: 'app-patient-schedule-overview',
  imports: [DatePipe, Tag],
  templateUrl: './patient-schedule-overview.component.html',
  styleUrl: './patient-schedule-overview.component.css',
})
export class PatientScheduleOverviewComponent {
  overview = input.required<PatientScheduleOverviewDto>();
  protected readonly slotStatusEnum = SlotStatus;

  protected statusSeverity(status: SlotStatus): 'success' | 'info' | 'danger' | 'secondary' {
    switch (status) {
      case SlotStatus.Completed: return 'success';
      case SlotStatus.Booked: return 'info';
      case SlotStatus.NoShow: return 'secondary';
      case SlotStatus.Cancelled: return 'danger';
      default: return 'secondary';
    }
  }

  protected statusLabel(status: SlotStatus): string {
    switch (status) {
      case SlotStatus.Completed: return 'Completed';
      case SlotStatus.Booked: return 'Booked';
      case SlotStatus.NoShow: return 'No Show';
      case SlotStatus.Cancelled: return 'Cancelled';
      default: return 'Unknown';
    }
  }
}
