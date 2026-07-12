import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { Appointment, shortId } from '../schedule.models';

@Component({
  selector: 'app-appointment-card',
  standalone: true,
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
  dragStarted = output<{ appointment: Appointment; clientY: number ; clientX:number}>();
  resizeStarted = output<{ appointment: Appointment; clientY: number }>();
  quickComplete = output<Appointment>();
  quickCancel = output<Appointment>();

  protected readonly shortId = shortId;

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

// Only this method changes — everything else in the file stays as-is.
protected onDragHandlePointerDown(event: PointerEvent): void {
  event.stopPropagation();
  this.dragStarted.emit({
    appointment: this.appointment(),
    clientY: event.clientY,
    clientX: event.clientX // added — needed for horizontal (day-to-day) drag resolution
  });
}

  protected onResizeHandlePointerDown(event: PointerEvent): void {
    event.stopPropagation();
    this.resizeStarted.emit({ appointment: this.appointment(), clientY: event.clientY });
  }

  protected onQuickComplete(event: Event): void {
    event.stopPropagation();
    this.quickComplete.emit(this.appointment());
  }

  protected onQuickCancel(event: Event): void {
    event.stopPropagation();
    this.quickCancel.emit(this.appointment());
  }
}