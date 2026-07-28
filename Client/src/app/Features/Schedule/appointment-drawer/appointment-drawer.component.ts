import { Component, ChangeDetectionStrategy, input, output, computed, inject, signal } from '@angular/core';
import { Appointment } from '../schedule.models';
import { OwnerDirectoryService } from '../../../Core/Services/owner-directory.service';
import { ConfirmDialogComponent, ConfirmDialogTone } from '../ConfirmDialogComponent/ConfirmDialogComponent';

type ActionKind = 'complete' | 'cancel' | 'noShow' | 'delete';
type PendingAction = { kind: ActionKind; title: string; message: string; tone: ConfirmDialogTone; confirmLabel: string } | null;

@Component({
  selector: 'app-appointment-drawer',
  standalone: true,
  imports: [ConfirmDialogComponent],
  templateUrl: './appointment-drawer.component.html',
  styleUrl: './appointment-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppointmentDrawerComponent {
  appointment = input<Appointment | null>(null);
  isOpen = input<boolean>(false);

  closeRequested = output<void>();
  completeRequested = output<string>();
  cancelRequested = output<string>();
  noShowRequested = output<string>();
  deleteRequested = output<string>();
  rescheduleRequested = output<Appointment>();

  private readonly ownerDirectory = inject(OwnerDirectoryService);
  protected readonly owner = computed(() => {
    const a = this.appointment();
    return a ? this.ownerDirectory.resolveOwner(a) : null;
  });

  protected readonly pendingAction = signal<PendingAction>(null);

  protected readonly durationLabel = computed(() => {
    const a = this.appointment();
    if (!a) return '';
    const minutes = (a.slotEnd.getTime() - a.slotStart.getTime()) / 60000;
    return minutes < 60 ? `${minutes} min` : `${(minutes / 60).toFixed(minutes % 60 === 0 ? 0 : 1)} hr`;
  });

  protected formatDateTime(date: Date): string {
    return date.toLocaleString('en-US', { weekday: 'short', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });
  }

  private ownerName(): string {
    return this.owner()?.name ?? 'this appointment';
  }

  protected requestComplete(): void {
    this.pendingAction.set({
      kind: 'complete', title: 'Complete appointment?',
      message: `Mark ${this.ownerName()}'s appointment as completed.`,
      tone: 'success', confirmLabel: 'Complete'
    });
  }

  protected requestCancel(): void {
    this.pendingAction.set({
      kind: 'cancel', title: 'Cancel appointment?',
      message: `${this.ownerName()}'s appointment will be cancelled. This can't be undone.`,
      tone: 'danger', confirmLabel: 'Cancel Appointment'
    });
  }

  protected requestNoShow(): void {
    this.pendingAction.set({
      kind: 'noShow', title: 'Mark as no-show?',
      message: `Mark ${this.ownerName()} as a no-show for this appointment.`,
      tone: 'warning', confirmLabel: 'Mark No Show'
    });
  }

  protected requestDelete(): void {
    this.pendingAction.set({
      kind: 'delete', title: 'Delete appointment?',
      message: `This permanently deletes ${this.ownerName()}'s appointment record. This can't be undone.`,
      tone: 'danger', confirmLabel: 'Delete'
    });
  }

  protected onConfirmed(): void {
    const action = this.pendingAction();
    const a = this.appointment();
    if (!action || !a) { this.pendingAction.set(null); return; }

    switch (action.kind) {
      case 'complete': this.completeRequested.emit(a.id); break;
      case 'cancel': this.cancelRequested.emit(a.id); break;
      case 'noShow': this.noShowRequested.emit(a.id); break;
      case 'delete': this.deleteRequested.emit(a.id); break;
    }
    this.pendingAction.set(null);
  }

  protected onCancelled(): void {
    this.pendingAction.set(null);
  }
}