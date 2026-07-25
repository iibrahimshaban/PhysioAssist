import { Component, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { RadioButton } from 'primeng/radiobutton';

@Component({
  selector: 'app-no-show-confirm-dialog',
  imports: [Dialog, RadioButton, FormsModule, Button],
  templateUrl: './no-show-confirm-dialog.component.html',
  styleUrl: './no-show-confirm-dialog.component.css',
})
export class NoShowConfirmDialogComponent {
  visible = false;
  patientNameSignal = signal('');

  patientName = this.patientNameSignal.asReadonly();
  countsAsUsed = true;

  confirmed = output<boolean>();
  cancelled = output<void>();

  get visibleModel(): boolean {
    return this.visible;
  }
  set visibleModel(value: boolean) {
    this.visible = value;
  }

  open(patientName: string): void {
    this.patientNameSignal.set(patientName);
    this.countsAsUsed = true;
    this.visible = true;
  }

   onConfirm(): void {
    this.visible = false;
    this.confirmed.emit(this.countsAsUsed);
  }
}
