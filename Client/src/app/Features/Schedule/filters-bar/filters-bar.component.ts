import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { ScheduleFilters, ScheduleSlotStatus } from '../schedule.models';

@Component({
  selector: 'app-filters-bar',
  standalone: true,
  templateUrl: './filters-bar.component.html',
  styleUrl: './filters-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FiltersBarComponent {
  filters = input.required<ScheduleFilters>();
  filtersChanged = output<Partial<ScheduleFilters>>();
  clearRequested = output<void>();

  protected readonly statusOptions: ScheduleSlotStatus[] = ['Booked', 'Completed', 'Cancelled', 'NoShow'];

  protected onSearchChange(event: Event): void {
    this.filtersChanged.emit({ patientSearch: (event.target as HTMLInputElement).value });
  }

  protected onToggleStatus(status: ScheduleSlotStatus): void {
    const current = new Set(this.filters().statuses);
    current.has(status) ? current.delete(status) : current.add(status);
    this.filtersChanged.emit({ statuses: current });
  }

  protected onToggleToday(): void {
    this.filtersChanged.emit({ showOnlyToday: !this.filters().showOnlyToday });
  }

  protected isActive(status: ScheduleSlotStatus): boolean {
    return this.filters().statuses.has(status);
  }
}