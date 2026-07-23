import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IntakeStatus } from '../../../models';

export interface StatusOption {
  label: string;
  value: IntakeStatus | null;
  count?: number;
}

@Component({
  selector: 'app-submission-filters-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './submission-filters-bar.component.html',
  styleUrl: './submission-filters-bar.component.css'
})
export class SubmissionFiltersBarComponent {
  @Input() searchTerm = '';
  @Input() selectedStatus: IntakeStatus | null = null;
  @Input() statusOptions: StatusOption[] = [];
  @Input() sortOption = 'newest';
  @Input() viewMode: 'cards' | 'table' = 'cards';
  @Input() mode: 'archive' | 'reception' = 'archive';

  @Output() searchChange = new EventEmitter<string>();
  @Output() statusChange = new EventEmitter<IntakeStatus | null>();
  @Output() clearFilters = new EventEmitter<void>();
  @Output() sortChange = new EventEmitter<string>();
  @Output() viewModeChange = new EventEmitter<'cards' | 'table'>();

  onSearchInput(val: string): void {
    this.searchChange.emit(val);
  }

  onClearSearch(): void {
    this.searchChange.emit('');
  }

  onStatusSelect(status: IntakeStatus | null): void {
    this.statusChange.emit(status);
  }

  onSortSelect(sort: string): void {
    this.sortChange.emit(sort);
  }

  onViewModeToggle(mode: 'cards' | 'table'): void {
    this.viewModeChange.emit(mode);
  }
}
