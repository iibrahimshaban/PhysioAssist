import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AskAiPanelStateService } from '../../../Core/Services/ask-ai-panel-state.service';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-ask-asi-button',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ask-asi-button.component.html',
  styleUrl: './ask-asi-button.component.css',
})
export class AskAsiButtonComponent {
  constructor(private readonly panelStateService: AskAiPanelStateService) {}

  togglePanel(): void {
    this.panelStateService.toggle();
  }
}
