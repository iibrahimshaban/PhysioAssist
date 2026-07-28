import { Component, ChangeDetectionStrategy, input, output, inject, effect, signal, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateAppointmentRequest, PatientOption } from '../schedule.models';
import { toIsoWithOffset } from '../../../Core/Services/schedule-page.service';
import { DoctorPatientService } from '../../../Core/Services/doctor-patient.service';
import { GuestService } from '../../../Core/Services/guest.service';
import { toSignal } from '@angular/core/rxjs-interop';

type PatientMode = 'existing' | 'guest';

@Component({
  selector: 'app-create-appointment-drawer',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './create-appointment-drawer.component.html',
  styleUrl: './create-appointment-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateAppointmentDrawerComponent {
  isOpen = input<boolean>(false);
  doctorId = input<string | null>(null);
  prefillStart = input<Date | null>(null);
  prefillEnd = input<Date | null>(null);
  prefillPatientId = input<string | null>(null);

  closeRequested = output<void>();
  createRequested = output<CreateAppointmentRequest>();

  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(DoctorPatientService);
  private readonly guestService = inject(GuestService);

 protected readonly form = this.fb.nonNullable.group({
    startTime: ['', Validators.required],
    durationMinutes: [30, Validators.required]
  });

  protected readonly guestForm = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    phoneNumber: ['', Validators.required]
  });

   private readonly formValid = toSignal(
    this.form.statusChanges,
    { initialValue: this.form.status }
  );
  private readonly guestFormValid = toSignal(
    this.guestForm.statusChanges,
    { initialValue: this.guestForm.status }
  );

  protected readonly durationOptions = [30, 45, 60, 90, 120];

  protected readonly patientMode = signal<PatientMode>('existing');
  protected readonly patients = signal<PatientOption[]>([]);
  protected readonly patientsLoading = signal(false);
  protected readonly patientsError = signal<string | null>(null);
  protected readonly patientSearchTerm = signal('');
  protected readonly selectedPatient = signal<PatientOption | null>(null);
  protected readonly isPatientListOpen = signal(false);

  // New — tracks the guest-creation network call itself, separate from
  // any other loading state, so the submit button can show "Creating..."
  // and be disabled specifically during that step.
  protected readonly isCreatingGuest = signal(false);
  protected readonly guestError = signal<string | null>(null);

  private patientsLoadedForDoctor: string | null = null;
  private prefillAppliedForPatientId: string | null = null;

  protected readonly filteredPatients = computed(() => {
    const term = this.patientSearchTerm().trim().toLowerCase();
    const list = this.patients();
    if (!term) return list;
    return list.filter(p => p.name.toLowerCase().includes(term));
  });

   protected readonly canSubmit = computed(() => {
    if (this.formValid() !== 'VALID') return false;
    if (this.isCreatingGuest()) return false;
    if (this.patientMode() === 'guest') return this.guestFormValid() === 'VALID';
    return this.selectedPatient() !== null;
  });


  constructor() {
    effect(() => {
      const start = this.prefillStart();
      if (start && this.isOpen()) {
        this.form.patchValue({ startTime: this.toTimeInputValue(start) });
      }
    });

    effect(() => {
      const open = this.isOpen();
      const doctorId = this.doctorId();
      if (open && doctorId && this.patientsLoadedForDoctor !== doctorId) {
        this.loadPatients(doctorId);
      }
    });

    effect(() => {
      const prefillId = this.prefillPatientId();
      const list = this.patients();
      if (!prefillId || list.length === 0) return;
      if (this.prefillAppliedForPatientId === prefillId) return;
      const match = list.find(p => p.id === prefillId);
      if (match) {
        this.patientMode.set('existing');
        this.selectPatient(match);
        this.prefillAppliedForPatientId = prefillId;
      }
    });
  }

  private async loadPatients(doctorId: string): Promise<void> {
    this.patientsLoading.set(true);
    this.patientsError.set(null);
    try {
      const list = await this.patientService.getPatientsForDoctor(doctorId);
      this.patients.set(list);
      this.patientsLoadedForDoctor = doctorId;
    } catch {
      this.patientsError.set('Could not load patients. Try again.');
      this.patients.set([]);
    } finally {
      this.patientsLoading.set(false);
    }
  }

  protected setMode(mode: PatientMode): void {
    this.patientMode.set(mode);
    this.guestError.set(null);
    if (mode === 'guest') {
      this.selectedPatient.set(null);
      this.isPatientListOpen.set(false);
    } else {
      this.guestForm.reset({ fullName: '', phoneNumber: '' });
    }
  }

  protected onSearchInput(term: string): void {
    this.patientSearchTerm.set(term);
    this.selectedPatient.set(null);
    this.isPatientListOpen.set(true);
  }

  protected openPatientList(): void {
    if (this.patientMode() === 'existing') this.isPatientListOpen.set(true);
  }

  protected selectPatient(patient: PatientOption): void {
    this.selectedPatient.set(patient);
    this.patientSearchTerm.set(patient.name);
    this.isPatientListOpen.set(false);
  }

  protected closePatientList(): void {
    setTimeout(() => this.isPatientListOpen.set(false), 150);
  }

  protected selectDuration(minutes: number): void {
    this.form.patchValue({ durationMinutes: minutes });
  }

  protected async onSubmit(): Promise<void> {
    if (!this.canSubmit() || !this.doctorId() || !this.prefillStart()) return;

    const { startTime, durationMinutes } = this.form.getRawValue();
    const baseDate = this.prefillStart()!;
    const [h, m] = startTime.split(':').map(Number);
    const start = new Date(baseDate);
    start.setHours(h, m, 0, 0);
    const end = new Date(start.getTime() + durationMinutes * 60000);

    if (this.patientMode() === 'guest') {
      this.isCreatingGuest.set(true);
      this.guestError.set(null);
      try {
        const { fullName, phoneNumber } = this.guestForm.getRawValue();
        const guest = await this.guestService.createGuest({ fullName, phoneNumber });

        this.createRequested.emit({
          doctorId: this.doctorId()!,
          guestId: guest.id,
          slotStart: toIsoWithOffset(start),
          slotEnd: toIsoWithOffset(end)
        });
      } catch {
        // Guest creation failed — do NOT emit createRequested. The drawer
        // stays open so reception can retry, rather than silently trying
        // to book an appointment against a guest that doesn't exist.
        this.guestError.set('Could not save guest details. Try again.');
      } finally {
        this.isCreatingGuest.set(false);
      }
      return;
    }

    this.createRequested.emit({
      doctorId: this.doctorId()!,
      patientId: this.selectedPatient()!.id,
      slotStart: toIsoWithOffset(start),
      slotEnd: toIsoWithOffset(end)
    });
  }

  protected onCancel(): void {
    this.form.reset({ startTime: '', durationMinutes: 30 });
    this.guestForm.reset({ fullName: '', phoneNumber: '' });
    this.patientMode.set('existing');
    this.selectedPatient.set(null);
    this.patientSearchTerm.set('');
    this.isPatientListOpen.set(false);
    this.guestError.set(null);
    this.closeRequested.emit();
  }

  private toTimeInputValue(date: Date): string {
    return `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
  }
}