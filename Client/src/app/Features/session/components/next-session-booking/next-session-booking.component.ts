import { Component, effect, inject, input, signal } from '@angular/core';
import { NextSessionBookingService } from '../../../../Core/Services/next-session-booking.service';
import { Router } from '@angular/router';
import { NextSessionBookingState, SessionBookingRoundDto, SlotCandidateDto } from '../../../../Shared/Models/next-session-booking.model';
import { Button } from "primeng/button";
import { InputNumber } from "primeng/inputnumber";
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-next-session-booking',
  imports: [Button, InputNumber, DatePipe, FormsModule],
  templateUrl: './next-session-booking.component.html',
  styleUrl: './next-session-booking.component.css',
})
export class NextSessionBookingComponent {
  sessionId = input.required<string>();
  patientId = input<string | null>(null);
  nextScheduledSlotStart = signal<string | null>(null);

  private readonly service = inject(NextSessionBookingService);
  private readonly router = inject(Router);

  protected readonly stateEnum = NextSessionBookingState;

  isLoadingContext = signal(true);
  state = signal<NextSessionBookingState>(NextSessionBookingState.NotApplicable);
  packageId = signal<string | null>(null);

  showOverrides = signal(false);
  durationMinutesOverride = signal<number | null>(null);
  sessionsPerWeekOverride = signal<number | null>(null);
  minimumGapOverride = signal<number | null>(null);

  isSearching = signal(false);
  round = signal<SessionBookingRoundDto | null>(null);

  isExtending = signal(false);
  isConfirming = signal(false);
  bookedConfirmation = signal<SlotCandidateDto | null>(null);

  constructor() {
    effect(() => {
      const id = this.sessionId();
      if (id) this.loadContext(id);
    });
  }

  private loadContext(sessionId: string): void {
    this.isLoadingContext.set(true);
    this.service.getContext(sessionId).subscribe({
      next: (ctx) => {
        this.state.set(ctx.state);
        this.packageId.set(ctx.packageId);
        this.nextScheduledSlotStart.set(ctx.nextScheduledSlotStart);
        this.isLoadingContext.set(false);
      },
      error: () => this.isLoadingContext.set(false),
    });
  }

  onSearchSlots(): void {
    const pkgId = this.packageId();
    if (!pkgId) return;

    this.isSearching.set(true);
    this.round.set(null);

    const durationMinutes = this.durationMinutesOverride();

    this.service
      .getNextSessionCandidates(pkgId, {
        sessionDurationOverride: durationMinutes ? this.toTimeSpanString(durationMinutes) : null,
        sessionsPerWeekOverride: this.sessionsPerWeekOverride(),
        minimumGapOverrideDays: this.minimumGapOverride(),
      })
      .subscribe({
        next: (round) => {
          this.round.set(round);
          this.isSearching.set(false);
        },
        error: () => this.isSearching.set(false),
      });
  }

  onExtendAndSearch(): void {
    const pkgId = this.packageId();
    if (!pkgId) return;

    this.isExtending.set(true);
    this.service.extendPackage(pkgId).subscribe({
      next: () => {
        this.isExtending.set(false);
        this.state.set(NextSessionBookingState.CanBookNext);
        this.onSearchSlots();
      },
      error: () => this.isExtending.set(false),
    });
  }

  onPickSlot(candidate: SlotCandidateDto): void {
    const pkgId = this.packageId();
    if (!pkgId) return;

    this.isConfirming.set(true);
    this.service.confirmSlot(pkgId, candidate).subscribe({
      next: () => {
        this.isConfirming.set(false);
        this.bookedConfirmation.set(candidate);
        this.round.set(null);
      },
      error: () => this.isConfirming.set(false),
    });
  }

  onRedirectToReceptionist(): void {
    const pid = this.patientId();
    this.router.navigate(['/app/schedule'], pid ? { queryParams: { patientId: pid } } : undefined);
  }

  private toTimeSpanString(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}:00`;
  }

   hasNothingToShow(): boolean {
    return this.state() === NextSessionBookingState.NotApplicable && !this.nextScheduledSlotStart();
  }
}
