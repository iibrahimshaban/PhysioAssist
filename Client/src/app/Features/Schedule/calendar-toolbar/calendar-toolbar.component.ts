import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CalendarViewMode } from '../schedule.models';


@Component({
  selector: 'app-calendar-toolbar',
  standalone: true,
  templateUrl: './calendar-toolbar.component.html',
  styleUrl: './calendar-toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CalendarToolbarComponent {
  dateRangeLabel = input<string>('');
  viewMode = input<CalendarViewMode>('week');

  previousClicked = output<void>();
  nextClicked = output<void>();
  todayClicked = output<void>();
  viewModeChanged = output<CalendarViewMode>();

  protected readonly viewOptions: { mode: CalendarViewMode; label: string }[] = [
    { mode: 'day', label: 'Day' },
    { mode: 'week', label: 'Week' }
  ];

  protected onPrevious(): void { this.previousClicked.emit(); }
  protected onNext(): void { this.nextClicked.emit(); }
  protected onToday(): void { this.todayClicked.emit(); }
  protected onViewModeSelect(mode: CalendarViewMode): void { this.viewModeChanged.emit(mode); }
}