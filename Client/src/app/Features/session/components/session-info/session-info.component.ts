import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { SessionDetailsResponse } from '../../../../Shared/Models/session-details-response';

@Component({
  selector: 'app-session-info',
  imports: [DatePipe],
  templateUrl: './session-info.component.html',
  styleUrl: './session-info.component.css',
})
export class SessionInfoComponent {
  session = input<SessionDetailsResponse | null>(null);
  isOpen = input.required<boolean>();

  toggle = output<void>();

  onToggle() {
    this.toggle.emit();
  }
}
