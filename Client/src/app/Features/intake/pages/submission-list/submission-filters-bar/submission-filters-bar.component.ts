import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IntakeStatus } from '../../../models';

@Component({
  selector: 'app-submission-filters-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './submission-filters-bar.component.html',
  styleUrl: './submission-filters-bar.component.css'
})
export class SubmissionFiltersBarComponent {
  @Input({ required: true }) searchTerm = '';
  @Input({ required: true }) selectedStatus: IntakeStatus | null = null;
  @Input({ required: true }) statusOptions: { label: string, value: IntakeStatus | null }[] = [];

  @Output() searchChange = new EventEmitter<string>();
  @Output() statusChange = new EventEmitter<IntakeStatus | null>();
  @Output() clearFilters = new EventEmitter<void>();
}
