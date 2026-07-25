import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { SessionDetailsResponse } from '../../../../Shared/Models/session-details-response';

@Component({
  selector: 'app-session-header',
  imports: [DatePipe],
  templateUrl: './session-header.component.html',
  styleUrl: './session-header.component.css',
})
export class SessionHeaderComponent {
  session = input<SessionDetailsResponse | null>(null);

  getStatusText(status?: number): string {
    switch (status) {
      case 0:
        return 'scheduled';

      case 1:
        return 'in-progress';

      case 2:
        return 'completed';

      case 3:
        return 'cancelled';

      default:
        return '';
    }
  }
}
