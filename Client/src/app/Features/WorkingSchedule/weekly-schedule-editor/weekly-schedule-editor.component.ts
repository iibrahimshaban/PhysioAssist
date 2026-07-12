import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { FormArray, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { DayOption } from '../WorkingSchedule.models';

export interface DayViewModel {
  index: number;
  label: string;
  enabled: boolean;
  durationMinutes: number | null;
  durationLabel: string | null;
  barLeftPct: number;
  barWidthPct: number;
}

@Component({
  selector: 'app-weekly-schedule-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './weekly-schedule-editor.component.html',
  styleUrl: './weekly-schedule-editor.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeeklyScheduleEditorComponent {
  @Input({ required: true }) weekDays!: readonly DayOption[];
  @Input({ required: true }) daysArray!: FormArray;
  @Input({ required: true }) dayViewModels!: DayViewModel[];

  readonly timelineTicks = ['12am', '6am', '12pm', '6pm', '12am'];

  dayGroup(index: number): FormGroup {
    return this.daysArray.at(index) as FormGroup;
  }

  hasRangeError(index: number): boolean {
    const group = this.dayGroup(index);
    const start = group.get('startTime');
    const end = group.get('endTime');
    return group.hasError('rangeInvalid') && !!(start?.touched || end?.touched);
  }

  isRequiredError(index: number, controlName: 'startTime' | 'endTime'): boolean {
    const control = this.dayGroup(index).get(controlName);
    return !!control?.hasError('required') && !!control.touched;
  }
}