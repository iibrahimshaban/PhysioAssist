import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';

export type ConfirmDialogTone = 'default' | 'danger' | 'warning' | 'success';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './ConfirmDialogComponent.html',
  styleUrl: './ConfirmDialogComponent.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmDialogComponent {
  isOpen = input<boolean>(false);
  title = input<string>('Are you sure?');
  message = input<string>('');
  confirmLabel = input<string>('Confirm');
  cancelLabel = input<string>('Cancel');
  tone = input<ConfirmDialogTone>('default');

  confirmed = output<void>();
  cancelled = output<void>();

  protected onConfirm(): void { this.confirmed.emit(); }
  protected onCancel(): void { this.cancelled.emit(); }
}