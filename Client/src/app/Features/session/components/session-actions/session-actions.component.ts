import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-session-actions',
  imports: [],
  templateUrl: './session-actions.component.html',
  styleUrl: './session-actions.component.css',
})
export class SessionActionsComponent {
  isSaving = input.required<boolean>();
  isCompleting = input.required<boolean>();

  save = output<void>();
  complete = output<void>();

  onSave() {
    this.save.emit();
  }

  onComplete() {
    this.complete.emit();
  }
}
