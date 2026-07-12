import { Component, EventEmitter, Output, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type Gender = 'male' | 'female' | '';

export const REFERRAL_SOURCE_OPTIONS = [
  'Social Media',
  'Friend or Family',
  'Google Search',
  'Doctor Referral',
  'Advertisement',
  'Other',
] as const;

export type ReferralSource = (typeof REFERRAL_SOURCE_OPTIONS)[number];

export interface PatientIntakeDefaults {
  fullName: string;
  dateOfBirth: string | null; // yyyy-MM-dd
  gender: Gender;
  email: string;
  phone: string;
  job: string;
  addressCity: string;
  freeTime: string;
  isMarried: boolean;
  referralSources: ReferralSource[];
}

@Component({
  selector: 'app-patient-intake-defaults',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './patient-intake-defaults.component.html',
})
export class PatientIntakeDefaultsComponent {
  /** Fires on every change so the parent page can keep a live copy of the payload. */
  @Output() detailsChange = new EventEmitter<PatientIntakeDefaults>();

  readonly fullName = signal('');
  readonly dateOfBirth = signal<string | null>(null);
  readonly gender = signal<Gender>('');
  readonly email = signal('');
  readonly phone = signal('');
  readonly job = signal('');
  readonly addressCity = signal('');
  readonly freeTime = signal('');
  readonly isMarried = signal(false);
  readonly referralSources = signal<Set<ReferralSource>>(new Set());

  readonly referralOptions = REFERRAL_SOURCE_OPTIONS;

  readonly nameTouched = signal(false);

  /** yyyy-MM-dd, used as the max for the date-of-birth input so a future date can't be picked. */
  readonly todayIso = new Date().toISOString().split('T')[0];

  readonly nameInvalid = computed(() => this.nameTouched() && this.fullName().trim().length === 0);

  readonly payload = computed<PatientIntakeDefaults>(() => ({
    fullName: this.fullName(),
    dateOfBirth: this.dateOfBirth(),
    gender: this.gender(),
    email: this.email(),
    phone: this.phone(),
    job: this.job(),
    addressCity: this.addressCity(),
    freeTime: this.freeTime(),
    isMarried: this.isMarried(),
    referralSources: Array.from(this.referralSources()),
  }));

  readonly isValid = computed(() => this.fullName().trim().length > 0);

  onFullNameChange(value: string): void {
    this.fullName.set(value);
    this.emitChange();
  }

  onFullNameBlur(): void {
    this.nameTouched.set(true);
  }

  onDateOfBirthChange(value: string): void {
    this.dateOfBirth.set(value || null);
    this.emitChange();
  }

  onGenderChange(value: string): void {
    this.gender.set(value as Gender);
    this.emitChange();
  }

  onEmailChange(value: string): void {
    this.email.set(value);
    this.emitChange();
  }

  onPhoneChange(value: string): void {
    this.phone.set(value);
    this.emitChange();
  }

  onJobChange(value: string): void {
    this.job.set(value);
    this.emitChange();
  }

  onAddressCityChange(value: string): void {
    this.addressCity.set(value);
    this.emitChange();
  }

  onFreeTimeChange(value: string): void {
    this.freeTime.set(value);
    this.emitChange();
  }

  onMarriedChange(checked: boolean): void {
    this.isMarried.set(checked);
    this.emitChange();
  }

  isReferralSelected(option: ReferralSource): boolean {
    return this.referralSources().has(option);
  }

  toggleReferralSource(option: ReferralSource, checked: boolean): void {
    const next = new Set(this.referralSources());
    if (checked) {
      next.add(option);
    } else {
      next.delete(option);
    }
    this.referralSources.set(next);
    this.emitChange();
  }

  private emitChange(): void {
    this.detailsChange.emit(this.payload());
  }
}