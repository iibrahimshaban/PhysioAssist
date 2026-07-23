import { Component, computed, input, output } from '@angular/core';
import { SessionBookingRoundDto } from '../SessionScheduling.model';
import { SlotCandidateDto } from '../../../Shared/Models/InitialReport.models';
import { DatePipe } from '@angular/common';
import { Button } from "primeng/button";

@Component({
  selector: 'app-slot-candidates-grid',
  imports: [DatePipe, Button],
  templateUrl: './slot-candidates-grid.component.html',
  styleUrl: './slot-candidates-grid.component.css',
})
export class SlotCandidatesGridComponent {
  round = input<SessionBookingRoundDto | null>(null);
  isConfirming = input<boolean>(false);

  pick = output<SlotCandidateDto>();
  manualSchedule = output<void>();  

  // Exposed for the container to read without re-deriving weekStart/weekEnd itself
  hasCandidates = computed(() => (this.round()?.candidates.length ?? 0) > 0);
}
