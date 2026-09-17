import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { SessionDetailsResponse } from '../../../../Shared/Models/session-details-response';

@Component({
  selector: 'app-session-header',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './session-header.component.html',
  styleUrl: './session-header.component.css',
})
export class SessionHeaderComponent {
  session = input<SessionDetailsResponse | null>(null);

  getStatusText(status?: number): string {
    switch (status) {
      case 0:
        return 'PATIENT_HEADER.STATUS_SCHEDULED';

      case 1:
        return 'PATIENT_HEADER.STATUS_IN_PROGRESS';

      case 2:
        return 'PATIENT_HEADER.STATUS_COMPLETED';

      case 3:
        return 'PATIENT_HEADER.STATUS_CANCELLED';

      default:
        return '';
    }
  }
}
