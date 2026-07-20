import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from "./Shared/Components/toast-container/toast-container.component";
import { LoadingBarComponent } from "./Shared/Components/loading-bar/loading-bar.component";
import { AskAiPanelStateService } from './Core/Services/ask-ai-panel-state.service';
import { AskAsiButtonComponent } from './Shared/Components/ask-asi-button/ask-asi-button.component';
import { AskAsiPanelComponent } from './Shared/Components/ask-asi-panel/ask-asi-panel.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastContainerComponent, LoadingBarComponent, AskAsiButtonComponent, AskAsiPanelComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Client');
}
