import { Component, ChangeDetectionStrategy, input, output, inject, effect, signal, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateAppointmentRequest, PatientOption } from '../schedule.models';
import { toIsoWithOffset } from '../../../Core/Services/schedule-page.service';
import { DoctorPatientService } from '../../../Core/Services/doctor-patient.service';

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

  // Set when arriving here from the receptionist booking flow via
  // /app/schedule?patientId=... — pre-selects this patient once the roster
  // for the doctor has loaded, so the receptionist doesn't have to search again.
  prefillPatientId = input<string | null>(null);

  closeRequested = output<void>();
  createRequested = output<CreateAppointmentRequest>();

  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(DoctorPatientService);

  protected readonly form = this.fb.nonNullable.group({
    startTime: ['', Validators.required],
    durationMinutes: [30, Validators.required]
  });

  protected readonly durationOptions = [30, 45, 60, 90, 120];

  protected readonly patientMode = signal<PatientMode>('existing');
  protected readonly patients = signal<PatientOption[]>([]);
  protected readonly patientsLoading = signal(false);
  protected readonly patientsError = signal<string | null>(null);
  protected readonly patientSearchTerm = signal('');
  protected readonly selectedPatient = signal<PatientOption | null>(null);
  protected readonly isPatientListOpen = signal(false);
  protected readonly guestId = signal<string | null>(null);

  private patientsLoadedForDoctor: string | null = null;
  // Guards against re-applying the prefill after the receptionist has
  // deliberately picked a different patient, and against re-triggering on
  // every unrelated signal change.
  private prefillAppliedForPatientId: string | null = null;

  protected readonly filteredPatients = computed(() => {
    const term = this.patientSearchTerm().trim().toLowerCase();
    const list = this.patients();
    if (!term) return list;
    return list.filter(p => p.name.toLowerCase().includes(term));
  });

  protected readonly canSubmit = computed(() => {
    if (this.form.invalid) return false;
    if (this.patientMode() === 'guest') return true;
    return this.selectedPatient() !== null;
  });

  constructor() {
    effect(() => {
      const start = this.prefillStart();
      if (start && this.isOpen()) {
        this.form.patchValue({ startTime: this.toTimeInputValue(start) });
      }
    });

    // Loads patients once per doctor per drawer session, not on every open
    effect(() => {
      const open = this.isOpen();
      const doctorId = this.doctorId();
      if (open && doctorId && this.patientsLoadedForDoctor !== doctorId) {
        this.loadPatients(doctorId);
      }
    });

    // Once the roster is loaded and a prefillPatientId was supplied, pre-select
    // that patient automatically — but only once per id, so it doesn't fight
    // the receptionist if she deliberately picks someone else afterward.
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
    if (mode === 'guest') {
      this.guestId.set(crypto.randomUUID());
      this.selectedPatient.set(null);
      this.isPatientListOpen.set(false);
    } else {
      this.guestId.set(null);
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
    // small delay so mousedown on an option fires before the list unmounts
    setTimeout(() => this.isPatientListOpen.set(false), 150);
  }

  protected selectDuration(minutes: number): void {
    this.form.patchValue({ durationMinutes: minutes });
  }

  protected onSubmit(): void {
    if (!this.canSubmit() || !this.doctorId() || !this.prefillStart()) return;

    const patientId = this.patientMode() === 'guest'
      ? this.guestId()!
      : this.selectedPatient()!.id;

    const { startTime, durationMinutes } = this.form.getRawValue();
    const baseDate = this.prefillStart()!;
    const [h, m] = startTime.split(':').map(Number);
    const start = new Date(baseDate);
    start.setHours(h, m, 0, 0);
    const end = new Date(start.getTime() + durationMinutes * 60000);

    this.createRequested.emit({
      doctorId: this.doctorId()!,
      patientId,
      slotStart: toIsoWithOffset(start),
      slotEnd: toIsoWithOffset(end)
    });
  }

  protected onCancel(): void {
    this.form.reset({ startTime: '', durationMinutes: 30 });
    this.patientMode.set('existing');
    this.selectedPatient.set(null);
    this.patientSearchTerm.set('');
    this.guestId.set(null);
    this.isPatientListOpen.set(false);
    this.closeRequested.emit();
  }

  private toTimeInputValue(date: Date): string {
    return `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
  }
}