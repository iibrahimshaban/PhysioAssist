import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { TodaySessionsService } from '../../Core/Services/today-sessions.service';
import { SlotBoardLane, TodaySessionCardDto, TodaySessionsOverviewDto } from '../../Shared/Models/today-sessions.model';
import { NoShowConfirmDialogComponent } from './no-show-confirm-dialog/no-show-confirm-dialog.component';
import { Button } from "primeng/button";
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-today-sessions-dashboard',
  imports: [NoShowConfirmDialogComponent, Button, DatePipe],
  templateUrl: './today-sessions-dashboard.component.html',
  styleUrl: './today-sessions-dashboard.component.css',
})
export class TodaySessionsDashboardComponent {
  private readonly router = inject(Router);
  private readonly todaySessionsService = inject(TodaySessionsService);
  protected readonly now = signal(new Date());

  protected readonly laneEnum = SlotBoardLane;
  private readonly noShowDialog = viewChild.required(NoShowConfirmDialogComponent);
  private noShowTargetSlotId: string | null = null;

  overview = signal<TodaySessionsOverviewDto | null>(null);
  isLoading = signal(false);
  actioningSlotId = signal<string | null>(null);

  formattedDate = computed(() => {
    const ov = this.overview();
    if (!ov) return '';
    return new Date(ov.date).toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' });
  });

  timelineRange = computed(() => {
    const ov = this.overview();
    if (!ov || ov.timeline.length === 0) return null;

    const starts = ov.timeline.map(t => new Date(t.slotStart).getTime());
    const min = Math.min(...starts);
    const max = Math.max(...starts);
    return { start: min - 60 * 60 * 1000, end: max + 60 * 60 * 1000 };
  });

  constructor() {
    this.load();
  }

  private load(): void {
    this.isLoading.set(true);
    this.todaySessionsService.getTodaySessions().subscribe({
      next: ov => {
        this.overview.set(ov);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  protected timelinePosition(isoDate: string): number {
    const range = this.timelineRange();
    if (!range) return 0;
    const t = new Date(isoDate).getTime();
    const pct = ((t - range.start) / (range.end - range.start)) * 100;
    return Math.min(100, Math.max(0, pct));
  }

  protected nowPosition(): number {
    return this.timelinePosition(new Date().toISOString());
  }

  protected laneDotClass(lane: SlotBoardLane): string {
    switch (lane) {
      case SlotBoardLane.Completed: return 'bg-green-500';
      case SlotBoardLane.InProgress: return 'bg-[#0B8EEA]';
      case SlotBoardLane.Missed: return 'bg-red-400';
      default: return 'bg-amber-400';
    }
  }

  onWeekView(): void {
    this.router.navigate(['/app/schedule']);
  }

  onGoToPatient(card: TodaySessionCardDto): void {
    this.router.navigate(['/app/patients', card.patientId, 'overview']);
  }

  onStartOrResume(card: TodaySessionCardDto): void {
    if (card.sessionId) {
      this.router.navigate(['/app/session', card.sessionId]);
      return;
    }

    this.actioningSlotId.set(card.slotId);
    this.todaySessionsService.startOrResumeSession(card.patientId, card.slotId).subscribe({
      next: res => {
        this.actioningSlotId.set(null);
        this.router.navigate(['/app/session', res.id]);
      },
      error: () => this.actioningSlotId.set(null),
    });
  }

  onLogPastSession(card: TodaySessionCardDto): void {
    // Same call as Start — StartOrResumeSessionAsync doesn't care whether the
    // slot's time has already passed, it just creates/opens the session.
    this.onStartOrResume(card);
  }

  onOpenNoShowDialog(card: TodaySessionCardDto): void {
    this.noShowTargetSlotId = card.slotId;
    this.noShowDialog().open(card.patientName);
  }

  onNoShowConfirmed(countsAsUsed: boolean): void {
    const slotId = this.noShowTargetSlotId;
    if (!slotId) return;

    this.actioningSlotId.set(slotId);
    this.todaySessionsService.markNoShow(slotId, countsAsUsed).subscribe({
      next: () => {
        this.actioningSlotId.set(null);
        this.load(); 
      },
      error: () => this.actioningSlotId.set(null),
    });
  }
}
