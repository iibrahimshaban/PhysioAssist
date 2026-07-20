import { Component, ChangeDetectionStrategy, input } from '@angular/core';

export type EmptyStateKind = 'no-doctor' | 'off-today' | 'no-appointments' | 'fully-booked';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmptyStateComponent {
  kind = input.required<EmptyStateKind>();

  protected readonly copy: Record<EmptyStateKind, { title: string; subtitle: string; icon: string }> = {
    'no-doctor': { title: 'Select a doctor', subtitle: 'Choose a doctor above to view their schedule.', icon: '🩺' },
    'off-today': { title: 'Doctor off today', subtitle: 'This doctor has no working hours configured for this day.', icon: '🌙' },
    'no-appointments': { title: 'No appointments', subtitle: 'This day is wide open. Click an available slot to book one.', icon: '📅' },
    'fully-booked': { title: 'Fully booked', subtitle: 'Every available slot today is taken.', icon: '✅' }
  };
}