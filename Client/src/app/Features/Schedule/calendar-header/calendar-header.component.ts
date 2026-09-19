import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

export interface CalendarDayColumn {
  date: Date;
  label: string;
  dayNumber: number;
  isToday: boolean;
}

@Component({
  selector: 'app-calendar-header',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './calendar-header.component.html',
  styleUrl: './calendar-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarHeaderComponent {
  days = input.required<CalendarDayColumn[]>();
  timelineWidth = input<number>(64);
}
