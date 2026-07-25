import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { DaysOfWeekFlags, PreferredTimeOfDay, SchedulingPriority, SlotFitType, TreatmentSchedulePlanResponse, TreatmentSchedulePlanStatus, UpsertTreatmentSchedulePlanRequest } from '../../../Shared/Models/InitialReport.models';
import { SnackbarService } from '../../../Core/Services/snackbar.service';
import { InitialReportService } from '../../../Core/Services/initial-report.service';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { SelectModule } from 'primeng/select';
import { MultiSelect } from 'primeng/multiselect';

@Component({
  selector: 'app-schedule-requirements',
  imports: [CommonModule, FormsModule, ButtonModule, SelectModule, MultiSelect],
  templateUrl: './schedule-requirements.component.html',
  styleUrl: './schedule-requirements.component.css',
})
export class ScheduleRequirementsComponent implements OnInit {
  @Input({ required: true }) reportId!: string;
  @Input() readonlyMode = false;
 
  @Output() planChanged = new EventEmitter<TreatmentSchedulePlanResponse>();
 
  constructor(
    private readonly initialReportService: InitialReportService,
    private readonly snackbar: SnackbarService,
    private readonly confirmationService: ConfirmationService,
  ) {}
 
  readonly TreatmentSchedulePlanStatus = TreatmentSchedulePlanStatus;
  readonly SlotFitType = SlotFitType;
 
  plan = signal<TreatmentSchedulePlanResponse | null>(null);
  loading = signal(false);
  saving = signal(false);
  booking = signal(false);
  sendingToReceptionist = signal(false);
  selectedCandidateIndex = signal<number | null>(null);
 
  totalSessions = signal<number | null>(null);
  sessionDurationMinutes = signal<number | null>(null);
  sessionsPerWeek = signal(3);
  minimumGapBetweenSessionsDays = signal(2);
  preferredTimeOfDay = signal<PreferredTimeOfDay>(PreferredTimeOfDay.Unspecified);
  selectedPreferredDays = signal<number[]>([]);
  priority = signal<SchedulingPriority>(SchedulingPriority.Normal);
 
  readonly totalSessionsOptions = [1,2,3,4,5, 6, 8, 10, 12, 15, 20].map(n => ({ label: `${n} sessions`, value: n }));
  readonly sessionDurationOptions = [30, 45, 60, 75, 90].map(m => ({ label: `${m} min`, value: m }));
  readonly sessionsPerWeekOptions = [1, 2, 3, 4, 5].map(n => ({ label: `${n}/week`, value: n }));
  readonly minimumGapOptions = [0, 1, 2, 3, 4, 5, 7].map(n => ({ label: `${n} day${n === 1 ? '' : 's'}`, value: n }));
  readonly preferredTimeOfDayOptions = [
  { label: 'No preference', value: PreferredTimeOfDay.Unspecified },
  { label: 'Morning (6:00 AM – 12:00 PM)', value: PreferredTimeOfDay.Morning },
  { label: 'Afternoon (12:00 PM – 5:00 PM)', value: PreferredTimeOfDay.Afternoon },
  { label: 'Evening (5:00 PM – 10:00 PM)', value: PreferredTimeOfDay.Evening },
];
  readonly preferredDaysOptions = [
    { label: 'Sunday', value: DaysOfWeekFlags.Sunday },
    { label: 'Monday', value: DaysOfWeekFlags.Monday },
    { label: 'Tuesday', value: DaysOfWeekFlags.Tuesday },
    { label: 'Wednesday', value: DaysOfWeekFlags.Wednesday },
    { label: 'Thursday', value: DaysOfWeekFlags.Thursday },
    { label: 'Friday', value: DaysOfWeekFlags.Friday },
    { label: 'Saturday', value: DaysOfWeekFlags.Saturday },
  ];
  readonly priorityOptions = [
    { label: 'Normal', value: SchedulingPriority.Normal },
    { label: 'Low', value: SchedulingPriority.Low },
    { label: 'High', value: SchedulingPriority.High },
    { label: 'Urgent', value: SchedulingPriority.Urgent },
  ];
 
  ngOnInit(): void {
    this.loadExistingPlan();
  }
 
  private loadExistingPlan(): void {
    this.loading.set(true);
    this.initialReportService.getSchedulePlan(this.reportId).subscribe({
      next: plan => {
        this.loading.set(false);
        this.applyPlan(plan);
      },
      error: err => {
        this.loading.set(false);
        // 404 just means no plan exists yet for this report — normal, not an error.
        if (err.status !== 404) {
          console.error('Failed to load schedule plan', err);
        }
      },
    });
  }
 
  private applyPlan(plan: TreatmentSchedulePlanResponse): void {
    this.plan.set(plan);
    this.totalSessions.set(plan.totalSessions || null);
    this.sessionDurationMinutes.set(plan.sessionDurationMinutes || null);
    this.sessionsPerWeek.set(plan.sessionsPerWeek);
    this.minimumGapBetweenSessionsDays.set(plan.minimumGapBetweenSessionsDays);
    this.preferredTimeOfDay.set(plan.preferredTimeOfDay);
    this.selectedPreferredDays.set(this.flagsToArray(plan.preferredDays));
    this.priority.set(plan.priority);
    this.selectedCandidateIndex.set(null);
    this.planChanged.emit(plan);
  }
 
  private flagsToArray(flags: DaysOfWeekFlags): number[] {
    return this.preferredDaysOptions.map(o => o.value).filter(v => (flags & v) !== 0);
  }
 
  private arrayToFlags(values: number[]): number {
    return values.reduce((acc, v) => acc | v, 0);
  }
 
  get isPending(): boolean {
    return !this.plan() || this.plan()!.status === TreatmentSchedulePlanStatus.Pending;
  }
 
  searchSlots(): void {
    if (!this.totalSessions() || !this.sessionDurationMinutes()) {
      this.snackbar.error('Missing fields', ['Total sessions and session duration are required.']);
      return;
    }
 
    this.saving.set(true);
 
    const request: UpsertTreatmentSchedulePlanRequest = {
      totalSessions: this.totalSessions()!,
      sessionDurationMinutes: this.sessionDurationMinutes()!,
      sessionsPerWeek: this.sessionsPerWeek(),
      minimumGapBetweenSessionsDays: this.minimumGapBetweenSessionsDays(),
      preferredTimeOfDay: this.preferredTimeOfDay(),
      preferredDays: this.arrayToFlags(this.selectedPreferredDays()),
      priority: this.priority(),
    };
 
    this.initialReportService.upsertSchedulePlan(this.reportId, request).subscribe({
      next: plan => {
        this.saving.set(false);
        this.applyPlan(plan);
        if (plan.candidateSlots.length === 0) {
          this.snackbar.error('No slots found', [
            "Try widening the preferred days/time, or check the doctor's working schedule.",
          ]);
        }
      },
      error: err => {
        this.saving.set(false);
        console.error('Failed to save schedule requirements', err);
        this.snackbar.error('Save failed', [this.getApiErrorDetail(err) || 'Unable to save schedule requirements.']);
      },
    });
  }
 
  selectCandidate(index: number): void {
    this.selectedCandidateIndex.set(index);
  }
 
  confirmBooking(): void {
    const index = this.selectedCandidateIndex();
    const plan = this.plan();
    if (index === null || !plan) return;
 
    const candidate = plan.candidateSlots[index];
 
    this.confirmationService.confirm({
      header: 'Confirm booking?',
      message: `Book the session for ${this.formatSlotTime(candidate.start)}?`,
      icon: 'pi pi-calendar-plus',
      acceptLabel: 'Book',
      rejectLabel: 'Cancel',
      accept: () => {
        this.booking.set(true);
        this.initialReportService
          .bookSchedulePlan(this.reportId, { slotStart: candidate.start, slotEnd: candidate.end })
          .subscribe({
            next: plan => {
              this.booking.set(false);
              this.applyPlan(plan);
              this.snackbar.success('Session booked');
            },
            error: err => {
              this.booking.set(false);
              console.error('Booking failed', err);
              this.snackbar.error('Booking failed', [this.getApiErrorDetail(err) || 'Unable to book this slot.']);
            },
          });
      },
    });
  }
 
  sendToReceptionist(): void {
    this.confirmationService.confirm({
      header: 'Send to receptionist?',
      message: 'The receptionist will contact the patient to confirm a session time.',
      icon: 'pi pi-send',
      acceptLabel: 'Send',
      rejectLabel: 'Cancel',
      accept: () => {
        this.sendingToReceptionist.set(true);
        this.initialReportService.sendSchedulePlanToReceptionist(this.reportId).subscribe({
          next: plan => {
            this.sendingToReceptionist.set(false);
            this.applyPlan(plan);
            this.snackbar.success('Sent to receptionist');
          },
          error: err => {
            this.sendingToReceptionist.set(false);
            console.error('Send to receptionist failed', err);
            this.snackbar.error('Failed', [this.getApiErrorDetail(err) || 'Unable to send to receptionist.']);
          },
        });
      },
    });
  }
 
  formatSlotTime(iso: string): string {
    return new Date(iso).toLocaleString(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }
 
  fitTypeLabel(fitType: SlotFitType): string {
    switch (fitType) {
      case SlotFitType.Exact:
        return 'Exact fit';
      case SlotFitType.LongerThanRequested:
        return 'Extra room after';
      case SlotFitType.ShorterThanRequested:
        return 'Shorter than requested';
      default:
        return '';
    }
  }
 
  // TODO: mirror your parent component's real implementation exactly — this is a
  // reasonable guess (PrimeNG-style ProblemDetails body: { detail: string }) based
  // on ResultExtensions.ToProblem() on the backend, not confirmed against your
  // actual getApiErrorDetail().
  private getApiErrorDetail(err: any): string | null {
    return err?.error?.detail ?? null;
  }
  togglePreferredDay(value: number): void {
    this.selectedPreferredDays.update(days =>
      days.includes(value) ? days.filter(d => d !== value) : [...days, value]
    );
  }
}
