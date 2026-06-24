import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-server-error',
  imports: [],
  templateUrl: './server-error.component.html',
  styleUrl: './server-error.component.css',
})
export class ServerErrorComponent {
  error = signal<any>(undefined);

  constructor() {
    const raw: string = history.state['error'];
    if (raw) {
      const lines = raw.split('\n');
      this.error.set({
        message: lines[0],        // "System.Exception: This is a test exception"
        details: lines.slice(1).join('\n')   // rest is the stack trace
      });
    }
  }

  reload() {
    window.location.reload();
  }
}
