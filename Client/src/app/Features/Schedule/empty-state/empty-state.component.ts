import {
  ChangeDetectionStrategy,
  Component,
  input,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';

export type EmptyStateKind =
  | 'no-doctor'
  | 'off-today'
  | 'no-appointments'
  | 'fully-booked';

interface EmptyStateCopy {
  title: string;
  subtitle: string;
  icon: string;
}

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [
    RouterLink,
    NgClass,
  ],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyStateComponent {
  kind = input.required<EmptyStateKind>();

  workingHoursRoute = input<string>('/app/working-schedule');

  protected readonly copy: Record<EmptyStateKind, EmptyStateCopy> = {
    'no-doctor': {
      title: 'Select a doctor',
      subtitle: 'Choose a doctor above to view their schedule.',
      icon: 'pi-user',
    },

    'off-today': {
      title: 'Doctor off today',
      subtitle: 'This doctor has no working hours configured for this day.',
      icon: 'pi-moon',
    },

    'no-appointments': {
      title: 'No appointments',
      subtitle: 'This day is wide open. Click an available slot to book one.',
      icon: 'pi-calendar',
    },

    'fully-booked': {
      title: 'Fully booked',
      subtitle: 'Every available slot today is taken.',
      icon: 'pi-check-circle',
    },
  };
}