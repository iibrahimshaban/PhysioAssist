import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { TagModule } from 'primeng/tag';
import { DatePickerModule } from 'primeng/datepicker';
import { StaffService } from '../../Core/Services/staff.service';
import { Receptionist, ReceptionistShiftType, SHIFT_DEFAULTS } from '../../Shared/Models/staff.model';

@Component({
  selector: 'app-staff',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    DialogModule,
    SelectModule,
    CheckboxModule,
    TagModule,
    DatePickerModule,
  ],
  templateUrl: './staff.component.html',
})
export class StaffComponent implements OnInit {
  private readonly staffService = inject(StaffService);
  private readonly fb = inject(FormBuilder);

  receptionists = this.staffService.receptionists;
  availablePermissions = this.staffService.availablePermissions;
  isLoading = this.staffService.isLoading;

  isModalOpen = signal(false);
  isSaving = signal(false);
  editingId = signal<string | null>(null);

  // Labels stay static ("Morning") — the actual times are shown live, driven by fromTime/toTime.
  shiftOptions = [
    { label: 'Morning', value: ReceptionistShiftType.Morning },
    { label: 'Evening', value: ReceptionistShiftType.Evening },
    { label: 'Full day', value: ReceptionistShiftType.FullDay },
  ];

  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', Validators.required],
    shift: [ReceptionistShiftType.Morning, Validators.required],
    fromTime: [null as Date | null, Validators.required],
    toTime: [null as Date | null, Validators.required],
    password: [''],
    newPassword: [''],
    permissions: [[] as string[]],
  });

  ngOnInit(): void {
    this.staffService.loadReceptionists();
    this.staffService.loadPermissions();
  }

  get isEditing(): boolean {
    return this.editingId() !== null;
  }

  // ─── Shift / time helpers ───────────────────────────────────────────────

  onShiftChange(shift: ReceptionistShiftType): void {
    const defaults = SHIFT_DEFAULTS[shift];
    this.form.patchValue({
      fromTime: this.timeStringToDate(defaults.from),
      toTime: this.timeStringToDate(defaults.to),
    });
  }

  private timeStringToDate(hhmmss: string | null | undefined): Date | null {
    if (!hhmmss || typeof hhmmss !== 'string') return null;

    const match = hhmmss.match(/^(\d{1,2}):(\d{1,2})/);
    if (!match) return null;

    const h = Number(match[1]);
    const m = Number(match[2]);
    if (isNaN(h) || isNaN(m)) return null;

    const d = new Date();
    d.setHours(h, m, 0, 0);
    return d;
  }

  private dateToTimeString(d: Date | null): string | null {
    if (!d) return null;
    const hh = d.getHours().toString().padStart(2, '0');
    const mm = d.getMinutes().toString().padStart(2, '0');
    return `${hh}:${mm}:00`;
  }

  /** Formats a Date (from the p-datepicker control) as "8:00 AM" */
  private formatTimeDate(d: Date | null): string {
    if (!d) return '--:--';
    return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
  }

  /** Formats a "HH:mm:ss" string (as returned by the API) as "8:00 AM" */
  private formatTimeString(hhmmss: string | null): string {
    return this.formatTimeDate(this.timeStringToDate(hhmmss));
  }

  private shiftName(shift: ReceptionistShiftType | null | undefined): string {
    return this.shiftOptions.find(o => o.value === shift)?.label ?? '';
  }

  /** Live label shown in the modal, reflecting whatever From/To are currently set to. */
  get currentShiftLabel(): string {
    const shift = this.form.get('shift')?.value;
    const from = this.form.get('fromTime')?.value ?? null;
    const to = this.form.get('toTime')?.value ?? null;
    if (shift === null || shift === undefined) return '';
    return `${this.shiftName(shift)} (${this.formatTimeDate(from)} – ${this.formatTimeDate(to)})`;
  }

  /** Label for a receptionist row in the list — uses their actual saved from/to, not shift defaults. */
  shiftLabel(r: Receptionist): string {
    return `${this.shiftName(r.shift)} (${this.formatTimeString(r.from)} – ${this.formatTimeString(r.to)})`;
  }

  get shiftTimesInvalid(): boolean {
    const from = this.form.get('fromTime')?.value;
    const to = this.form.get('toTime')?.value;
    return !!from && !!to && to <= from;
  }

  // ─── Modal open/close ────────────────────────────────────────────────────

  openCreateModal(): void {
    this.editingId.set(null);
    const defaultShift = ReceptionistShiftType.Morning;
    const defaults = SHIFT_DEFAULTS[defaultShift];

    this.form.reset({
      shift: defaultShift,
      fromTime: this.timeStringToDate(defaults.from),
      toTime: this.timeStringToDate(defaults.to),
      permissions: [],
    });
    this.form.get('password')?.setValidators([Validators.required, Validators.minLength(8)]);
    this.form.get('email')?.enable();
    this.form.get('password')?.updateValueAndValidity();
    this.isModalOpen.set(true);
  }

  openEditModal(r: Receptionist): void {
    this.editingId.set(r.id);
    const [firstName, ...rest] = r.fullName.split(' ');

    this.form.reset({
      firstName,
      lastName: rest.join(' '),
      email: r.email,
      phone: r.phone,
      shift: r.shift,
      fromTime: this.timeStringToDate(r.from),
      toTime: this.timeStringToDate(r.to),
      permissions: [...r.permissions],
    });

    this.form.get('password')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();
    this.form.get('email')?.disable();
    this.isModalOpen.set(true);
  }

  closeModal(): void {
    this.isModalOpen.set(false);
  }

  // ─── Permissions ─────────────────────────────────────────────────────────

  togglePermission(value: string): void {
    const current = this.form.get('permissions')?.value ?? [];
    const next = current.includes(value)
      ? current.filter((p: string) => p !== value)
      : [...current, value];
    this.form.get('permissions')?.setValue(next);
  }

  isPermissionChecked(value: string): boolean {
    return (this.form.get('permissions')?.value ?? []).includes(value);
  }

  permissionTitle(value: string): string {
    return this.availablePermissions().find(p => p.value === value)?.title ?? value;
  }

  // ─── Save / delete / toggle ──────────────────────────────────────────────

  save(): void {
    if (this.form.invalid || this.shiftTimesInvalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const v = this.form.getRawValue();
    const from = this.dateToTimeString(v.fromTime);
    const to = this.dateToTimeString(v.toTime);

    const request$ = this.isEditing
      ? this.staffService.update(this.editingId()!, {
          firstName: v.firstName!,
          lastName: v.lastName!,
          phone: v.phone!,
          shift: v.shift!,
          from,
          to,
          permissions: v.permissions!,
          newPassword: v.newPassword || null,
        })
      : this.staffService.create({
          firstName: v.firstName!,
          lastName: v.lastName!,
          email: v.email!,
          phone: v.phone!,
          password: v.password!,
          shift: v.shift!,
          from,
          to,
          permissions: v.permissions!,
        });

    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.isModalOpen.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  toggleDisabled(r: Receptionist): void {
    this.staffService.toggleDisabled(r.id).subscribe();
  }

  remove(r: Receptionist): void {
    if (!confirm(`Delete ${r.fullName}? This cannot be undone.`)) return;
    this.staffService.delete(r.id).subscribe();
  }

  initials(fullName: string): string {
    return fullName
      .split(' ')
      .map(p => p[0])
      .join('')
      .toUpperCase();
  }
}