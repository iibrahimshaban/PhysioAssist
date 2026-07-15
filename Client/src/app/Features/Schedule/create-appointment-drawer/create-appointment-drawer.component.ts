import { Component, ChangeDetectionStrategy, input, output, inject, effect } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateAppointmentRequest } from '../schedule.models';
import { toIsoWithOffset } from '../../../Core/Services/schedule-page.service';

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

  closeRequested = output<void>();
  createRequested = output<CreateAppointmentRequest>();

  private readonly fb = inject(FormBuilder);
  protected readonly form = this.fb.nonNullable.group({
    patientId: ['', Validators.required], // TEMPORARY: raw GUID entry — no patient search endpoint yet
    startTime: ['', Validators.required],
    durationMinutes: [30, Validators.required]
  });

  protected readonly durationOptions = [30, 45, 60, 90, 120];

  constructor() {
    effect(() => {
      const start = this.prefillStart();
      if (start && this.isOpen()) {
        this.form.patchValue({ startTime: this.toTimeInputValue(start) });
      }
    });
  }

  protected selectDuration(minutes: number): void {
    this.form.patchValue({ durationMinutes: minutes });
  }

  protected onSubmit(): void {
  if (this.form.invalid || !this.doctorId() || !this.prefillStart()) return;

  const { patientId, startTime, durationMinutes } = this.form.getRawValue();
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
    this.form.reset({ patientId: '', startTime: '', durationMinutes: 30 });
    this.closeRequested.emit();
  }

  private toTimeInputValue(date: Date): string {
    return `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
  }
}