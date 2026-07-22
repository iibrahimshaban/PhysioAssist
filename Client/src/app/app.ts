import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from "./Shared/Components/toast-container/toast-container.component";
import { LoadingBarComponent } from "./Shared/Components/loading-bar/loading-bar.component";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastContainerComponent, LoadingBarComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Client');
}
