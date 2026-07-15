import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { AvailableInterval } from '../schedule.models';

@Component({
  selector: 'app-availability-overlay',
  standalone: true,
  templateUrl: './availability-overlay.component.html',
  styleUrl: './availability-overlay.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AvailabilityOverlayComponent {
  interval = input.required<AvailableInterval>();
  top = input.required<number>();
  height = input.required<number>();
  intervalClicked = output<AvailableInterval>();

  protected onClick(): void { this.intervalClicked.emit(this.interval()); }
}