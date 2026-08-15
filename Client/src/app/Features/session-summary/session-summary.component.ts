import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SessionProgressNoteComponent } from '../session/components/session-progress-note/session-progress-note.component';

@Component({
  selector: 'app-session-summary',
  standalone: true,
  imports: [SessionProgressNoteComponent],
  templateUrl: './session-summary.component.html',
  styleUrl: './session-summary.component.css',
})
export class SessionSummaryComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly sessionId = this.route.snapshot.paramMap.get('id') ?? '';

  goBack(): void {
    this.router.navigate(['/app/session', this.sessionId]);
  }
}
