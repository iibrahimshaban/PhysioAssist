import { Component, Input } from '@angular/core';
import { NgClass } from '@angular/common';

/**
 * Intake-Module-local shared page container.
 *
 * SCOPE: this component lives INSIDE the Intake Module (Features/intake/shared)
 * and is used ONLY by Intake pages. It is NOT a global/shared component and does
 * not touch the navbar, global styles, or any other feature module.
 *
 * It enforces a single, consistent grid for the three Intake list pages
 * (Reception, Submissions, Intake Forms) so their page titles, stats cards,
 * search bars, filters, lists and cards all start/end at the same horizontal
 * positions and never shift when navigating between them. All width/gutter/
 * padding values are defined locally (no global design-system edits).
 *
 * @input flushTop  When true, drops the standard top padding (the page already
 *                  supplies its own header offset). Default false.
 */
@Component({
  selector: 'app-intake-page-container',
  standalone: true,
  imports: [NgClass],
  template: `
    <div class="intake-page-container" [ngClass]="{ 'intake-page-container--flush-top': flushTop }">
      <ng-content></ng-content>
    </div>
  `,
  styleUrl: './intake-page-container.component.css',
})
export class IntakePageContainerComponent {
  /** Drop the standard top padding when the page manages its own header offset. */
  @Input() flushTop = false;
}
