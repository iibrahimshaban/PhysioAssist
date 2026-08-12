import { Component, ChangeDetectionStrategy, input, output, inject, computed, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { Appointment } from '../schedule.models';
import { OwnerDirectoryService } from '../../../Core/Services/owner-directory.service';
import { ConfirmDialogComponent, ConfirmDialogTone } from '../ConfirmDialogComponent/ConfirmDialogComponent';

type PendingCardAction = { kind: 'complete' | 'cancel'; title: string; message: string; tone: ConfirmDialogTone } | null;

@Component({
  selector: 'app-appointment-card',
  standalone: true,
  // NgClass + TooltipModule added for the redesigned template (status icon,
  // pTooltip on the quick-action buttons). No existing bindings changed.
  imports: [ConfirmDialogComponent, NgClass, TooltipModule],
  templateUrl: './appointment-card.component.html',
  styleUrl: './appointment-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppointmentCardComponent {
  appointment = input.required<Appointment>();
  top = input.required<number>();
  height = input.required<number>();
  dragging = input<boolean>(false);

  cardClicked = output<Appointment>();
  dragStarted = output<{ appointment: Appointment; clientY: number; clientX: number }>();
  resizeStarted = output<{ appointment: Appointment; clientY: number }>();
  quickComplete = output<Appointment>();
  quickCancel = output<Appointment>();

  private readonly ownerDirectory = inject(OwnerDirectoryService);
  protected readonly owner = computed(() => this.ownerDirectory.resolveOwner(this.appointment()));

  protected readonly pendingAction = signal<PendingCardAction>(null);

  // Purely presentational — maps status to a PrimeIcon glyph for the small
  // accent icon; does not affect any scheduling logic.
  protected readonly statusIcon = computed(() => {
    switch (this.appointment().status) {
      case 'Completed': return 'pi-check-circle';
      case 'Cancelled': return 'pi-times-circle';
      case 'NoShow': return 'pi-exclamation-circle';
      default: return 'pi-calendar';
    }
  });

  protected get durationLabel(): string {
    const minutes = (this.appointment().slotEnd.getTime() - this.appointment().slotStart.getTime()) / 60000;
    return minutes < 60 ? `${minutes} min` : `${(minutes / 60).toFixed(minutes % 60 === 0 ? 0 : 1)} hr`;
  }

  protected get timeLabel(): string {
    return this.appointment().slotStart.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
  }

  protected onCardClick(): void {
    this.cardClicked.emit(this.appointment());
  }

  protected onDragHandlePointerDown(event: PointerEvent): void {
    event.stopPropagation();
    this.dragStarted.emit({
      appointment: this.appointment(),
      clientY: event.clientY,
      clientX: event.clientX
    });
  }

  protected onResizeHandlePointerDown(event: PointerEvent): void {
    event.stopPropagation();
    this.resizeStarted.emit({ appointment: this.appointment(), clientY: event.clientY });
  }

  protected onQuickComplete(event: Event): void {
    event.stopPropagation();
    this.pendingAction.set({
      kind: 'complete',
      title: 'Complete appointment?',
      message: `Mark ${this.owner().name}'s appointment as completed.`,
      tone: 'success'
    });
  }

  protected onQuickCancel(event: Event): void {
    event.stopPropagation();
    this.pendingAction.set({
      kind: 'cancel',
      title: 'Cancel appointment?',
      message: `This will cancel ${this.owner().name}'s appointment.`,
      tone: 'danger'
    });
  }

  protected onConfirmed(): void {
    const action = this.pendingAction();
    if (action?.kind === 'complete') this.quickComplete.emit(this.appointment());
    if (action?.kind === 'cancel') this.quickCancel.emit(this.appointment());
    this.pendingAction.set(null);
  }

  protected onCancelled(): void {
    this.pendingAction.set(null);
  }
}