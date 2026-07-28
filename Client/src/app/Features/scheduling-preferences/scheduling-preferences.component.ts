import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { CardModule } from 'primeng/card';
import { DoctorSchedulingPreferenceService } from '../../Core/Services/doctor-scheduling-preference.service';
import { SnackbarService } from '../../Core/Services/snackbar.service';
import { UpdateDoctorSchedulingPreferenceRequest } from '../../Shared/Models/Doctorschedulingpreference.model';
import { rxResource } from '@angular/core/rxjs-interop';


@Component({
  selector: 'app-scheduling-preferences',
  standalone: true,
  imports: [FormsModule, ButtonModule, InputNumberModule, ToggleSwitchModule, CardModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './scheduling-preferences.component.html',
})
export class SchedulingPreferencesComponent {
  private readonly preferenceService = inject(DoctorSchedulingPreferenceService);
  private readonly snackbar = inject(SnackbarService);

  protected readonly saving = signal(false);

  protected readonly preferenceResource = rxResource({
    stream: () => this.preferenceService.get(),
  });

  // Local editable copies — kept separate from the loaded resource so the
  // form doesn't fight the resource's own reload/error states while typing.
  protected readonly maxShortfallToleranceMinutes = signal(15);
  protected readonly maxDaysOutForExactMatch = signal(7);
  protected readonly allowShorterSlots = signal(true);

  private readonly hydrated = signal(false);

  protected readonly canSave = computed(() => !this.saving() && this.preferenceResource.hasValue());

  constructor() {
    // Hydrate the editable signals the first time the resource resolves.
    // Guarded by `hydrated` so a later refresh of the resource (e.g. if it
    // ever gets a manual .reload()) doesn't stomp on in-flight edits.
    effect(() => {
      const value = this.preferenceResource.value();
      if (value && !this.hydrated()) {
        this.hydrateFrom(value);
      }
    });
  }

  private hydrateFrom(value: { maxShortfallToleranceMinutes: number; maxDaysOutForExactMatch: number; allowShorterSlots: boolean }) {
    this.maxShortfallToleranceMinutes.set(value.maxShortfallToleranceMinutes);
    this.maxDaysOutForExactMatch.set(value.maxDaysOutForExactMatch);
    this.allowShorterSlots.set(value.allowShorterSlots);
    this.hydrated.set(true);
  }

  protected save(): void {
    const request: UpdateDoctorSchedulingPreferenceRequest = {
      maxShortfallToleranceMinutes: this.maxShortfallToleranceMinutes(),
      maxDaysOutForExactMatch: this.maxDaysOutForExactMatch(),
      allowShorterSlots: this.allowShorterSlots(),
    };

    this.saving.set(true);
    this.preferenceService.update(request).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.hydrateFrom(updated);
        this.snackbar.success('Scheduling preferences saved.');
      },
      error: () => {
        this.saving.set(false);
        this.snackbar.error('Could not save scheduling preferences. Try again.');
      },
    });
  }

  protected resetToDefaults(): void {
    this.maxShortfallToleranceMinutes.set(15);
    this.maxDaysOutForExactMatch.set(7);
    this.allowShorterSlots.set(true);
  }
}