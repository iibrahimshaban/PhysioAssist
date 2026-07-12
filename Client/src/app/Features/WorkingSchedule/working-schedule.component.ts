import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { WorkingScheduleService } from '../../Core/Services/working-schedule.service';
import { SnackbarService } from '../../Core/Services/snackbar.service';
import {
  WEEK_DAYS,
  WorkingScheduleDayRequest,
  WorkingScheduleDto,
} from './WorkingSchedule.models';
import {
  MINUTES_IN_DAY,
  formatMinutesLabel,
  toApiTimeString,
  toInputTimeString,
  toMinutes,
} from '../../Shared/Utils/time.utils';
import { DayViewModel, WeeklyScheduleEditorComponent } from './weekly-schedule-editor/weekly-schedule-editor.component';

type Preset = 'weekdays' | 'everyday' | 'clear';

/** Only meaningful while the day is enabled — required + range checks are skipped otherwise. */
function dayRangeValidator(group: AbstractControl): ValidationErrors | null {
  const enabled = group.get('enabled')?.value as boolean;
  if (!enabled) return null;

  const start = group.get('startTime')?.value as string;
  const end = group.get('endTime')?.value as string;
  if (!start || !end) return null;

  return start < end ? null : { rangeInvalid: true };
}

@Component({
  selector: 'app-working-schedule',
  standalone: true,
  imports: [ReactiveFormsModule, WeeklyScheduleEditorComponent],
  templateUrl: './working-schedule.component.html',
  styleUrl: './working-schedule.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkingScheduleComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly scheduleService = inject(WorkingScheduleService);
  private readonly snackbar = inject(SnackbarService);

  readonly weekDays = WEEK_DAYS;

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly processingAction = signal(false);
  readonly schedule = signal<WorkingScheduleDto | null>(null);

  readonly hasSchedule = computed(() => this.schedule() !== null);
  readonly submitLabel = computed(() => (this.hasSchedule() ? 'Save Changes' : 'Create Schedule'));
  readonly statusLabel = computed(() => {
    const s = this.schedule();
    if (!s) return null;
    return s.isActive ? 'Active' : 'Inactive';
  });

  readonly form: FormGroup = this.fb.group({
    days: this.fb.array(this.weekDays.map(() => this.buildDayGroup())),
  });

  get daysArray(): FormArray {
    return this.form.get('days') as FormArray;
  }

  // Live snapshot of the form, used to drive duration chips, the mini timeline
  // bars, and the weekly summary — recomputed automatically on every edit.
  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  readonly dayViewModels = computed<DayViewModel[]>(() => {
    const value = this.formValue() as { days: Array<{ enabled?: boolean; startTime?: string; endTime?: string }> };

    return this.weekDays.map((day, index) => {
      const raw = value.days[index];
      const enabled = !!raw?.enabled;
      const startTime = raw?.startTime ?? '';
      const endTime = raw?.endTime ?? '';
      const isValidRange = enabled && !!startTime && !!endTime && startTime < endTime;
      const durationMinutes = isValidRange ? toMinutes(endTime) - toMinutes(startTime) : null;

      return {
        index,
        label: day.label,
        enabled,
        durationMinutes,
        durationLabel: durationMinutes !== null ? formatMinutesLabel(durationMinutes) : null,
        barLeftPct: isValidRange ? (toMinutes(startTime) / MINUTES_IN_DAY) * 100 : 0,
        barWidthPct: isValidRange ? (durationMinutes! / MINUTES_IN_DAY) * 100 : 0,
      };
    });
  });

  readonly activeDaysCount = computed(() => this.dayViewModels().filter((d) => d.enabled).length);

  readonly totalWeeklyMinutes = computed(() =>
    this.dayViewModels().reduce((sum, vm) => sum + (vm.durationMinutes ?? 0), 0),
  );

  readonly totalWeeklyLabel = computed(() =>
    this.totalWeeklyMinutes() > 0 ? formatMinutesLabel(this.totalWeeklyMinutes()) : '0h',
  );

  readonly summaryLabel = computed(() => {
    const count = this.activeDaysCount();
    if (count === 0) return 'No working days selected yet';
    const dayWord = count === 1 ? 'day' : 'days';
    return `${count} ${dayWord} selected · ${this.totalWeeklyLabel()} per week`;
  });

  ngOnInit(): void {
    this.loadSchedule();
  }

  private buildDayGroup(): FormGroup {
    const group = this.fb.group(
      {
        enabled: this.fb.control(false),
        startTime: this.fb.control({ value: '', disabled: true }),
        endTime: this.fb.control({ value: '', disabled: true }),
      },
      { validators: dayRangeValidator },
    );

    // Reacts to both user clicks and programmatic patches (e.g. loading an
    // existing schedule or applying a preset), so the enable/disable +
    // required-validator logic lives in exactly one place.
group.get('enabled')?.valueChanges.subscribe(enabled => {this.syncDayControls(group, !!enabled);});

    return group;
  }

  private syncDayControls(group: FormGroup, enabled: boolean): void {
    const start = group.get('startTime')!;
    const end = group.get('endTime')!;

    if (enabled) {
      start.enable({ emitEvent: false });
      end.enable({ emitEvent: false });
      start.setValidators(Validators.required);
      end.setValidators(Validators.required);
    } else {
      start.disable({ emitEvent: false });
      end.disable({ emitEvent: false });
      start.clearValidators();
      end.clearValidators();
      start.setValue('', { emitEvent: false });
      end.setValue('', { emitEvent: false });
    }

    start.updateValueAndValidity({ emitEvent: false });
    end.updateValueAndValidity({ emitEvent: false });
  }

  private loadSchedule(): void {
    this.loading.set(true);

    this.scheduleService.getActiveSchedule().subscribe((schedule) => {
      this.schedule.set(schedule);
      if (schedule) {
        this.patchFormFromSchedule(schedule);
      }
      this.loading.set(false);
    });
  }

  private patchFormFromSchedule(schedule: WorkingScheduleDto): void {
    const byDay = new Map(schedule.days.map((d) => [d.day, d]));

    this.weekDays.forEach((day, index) => {
      const match = byDay.get(day.value);
      if (!match) return;

      this.daysArray.at(index).patchValue({
        enabled: true,
        startTime: toInputTimeString(match.startTime),
        endTime: toInputTimeString(match.endTime),
      });
    });
  }

  private collectSelectedDays(): WorkingScheduleDayRequest[] {
    return this.weekDays
      .map((day, index) => ({ day, group: this.daysArray.at(index) as FormGroup }))
      .filter(({ group }) => group.get('enabled')!.value)
      .map(({ day, group }) => ({
        day: day.value,
        startTime: toApiTimeString(group.get('startTime')!.value),
        endTime: toApiTimeString(group.get('endTime')!.value),
      }));
  }

  /** Quick-setup shortcuts so a doctor can configure a typical week in one click. */
  applyPreset(preset: Preset): void {
    const defaultStart = '09:00';
    const defaultEnd = '17:00';

    this.weekDays.forEach((day, index) => {
      const group = this.daysArray.at(index) as FormGroup;

      if (preset === 'clear') {
        group.patchValue({ enabled: false, startTime: '', endTime: '' });
        return;
      }

      const isWeekday = day.value >= 1 && day.value <= 5; // Monday(1) - Friday(5)
      const shouldEnable = preset === 'everyday' || isWeekday;

      if (!shouldEnable) {
        group.patchValue({ enabled: false, startTime: '', endTime: '' });
        return;
      }

      const current = group.getRawValue();
      group.patchValue({
        enabled: true,
        startTime: current.startTime || defaultStart,
        endTime: current.endTime || defaultEnd,
      });
    });

    this.form.markAsDirty();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const days = this.collectSelectedDays();

    if (days.length === 0) {
      this.snackbar.error('Select at least one working day.');
      return;
    }

    this.saving.set(true);
    const isUpdate = this.hasSchedule();

    const request$ = isUpdate
      ? this.scheduleService.updateDays(this.schedule()!.id, { days })
      : this.scheduleService.create({ days });

    request$.subscribe({
      next: (updated) => {
        this.schedule.set(updated);
        this.saving.set(false);
        this.snackbar.success(isUpdate ? 'Schedule updated.' : 'Schedule created.');
      },
      error: () => this.saving.set(false),
    });
  }

  onDelete(): void {
    const current = this.schedule();
    if (!current) return;
    if (!confirm('Delete this working schedule permanently? This cannot be undone.')) return;

    this.processingAction.set(true);
    this.scheduleService.delete(current.id).subscribe({
      next: () => {
        this.processingAction.set(false);
        this.schedule.set(null);
        this.resetForm();
        this.snackbar.success('Schedule deleted.');
      },
      error: () => this.processingAction.set(false),
    });
  }

  private resetForm(): void {
    this.weekDays.forEach((_, index) => {
      this.daysArray.at(index).patchValue({ enabled: false, startTime: '', endTime: '' });
    });
    this.form.markAsPristine();
    this.form.markAsUntouched();
  }
}