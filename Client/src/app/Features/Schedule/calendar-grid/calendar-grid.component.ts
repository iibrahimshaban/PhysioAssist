import {
  Component, ChangeDetectionStrategy, computed, input, output, signal,
  ElementRef, viewChild, AfterViewInit, OnDestroy
} from '@angular/core';
import { CalendarHeaderComponent, CalendarDayColumn } from '../calendar-header/calendar-header.component';
import { AppointmentCardComponent } from '../appointment-card/appointment-card.component';
import { AvailabilityOverlayComponent } from '../availability-overlay/availability-overlay.component';
import { Appointment, AvailableInterval, WorkingDayWindow, CalendarViewMode } from '../schedule.models';

interface DragState {
  appointment: Appointment;
  originalStart: Date;
  originalEnd: Date;
  startClientY: number;
  mode: 'move' | 'resize';
}

@Component({
  selector: 'app-calendar-grid',
  standalone: true,
  imports: [CalendarHeaderComponent, AppointmentCardComponent, AvailabilityOverlayComponent],
  templateUrl: './calendar-grid.component.html',
  styleUrl: './calendar-grid.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CalendarGridComponent implements AfterViewInit, OnDestroy {
  selectedDate = input.required<Date>();
  viewMode = input.required<CalendarViewMode>();
  appointments = input.required<Appointment[]>();
  availability = input.required<AvailableInterval[]>();
  workingDays = input.required<WorkingDayWindow[]>();

  appointmentClicked = output<Appointment>();
  intervalClicked = output<AvailableInterval>();
  rescheduleRequested = output<{ appointment: Appointment; newStart: Date; newEnd: Date }>();
  quickComplete = output<Appointment>();
  quickCancel = output<Appointment>();

  private readonly scrollContainer = viewChild<ElementRef<HTMLDivElement>>('scrollContainer');

  protected readonly timelineWidth = 64;
  protected readonly hourHeight = 64;
  protected readonly snapMinutes = 5;

  protected readonly draggingId = signal<string | null>(null);
  protected readonly liveDragPositions = signal<Map<string, { start: Date; end: Date }>>(new Map());
  private dragState: DragState | null = null;

  protected readonly now = signal(new Date());
  private nowTimer?: ReturnType<typeof setInterval>;

  constructor() {
    this.nowTimer = setInterval(() => this.now.set(new Date()), 60000);
  }

  ngAfterViewInit(): void {
    this.scrollToCurrentHour();
  }

  ngOnDestroy(): void {
    if (this.nowTimer) clearInterval(this.nowTimer);
  }

  private windowForDay(date: Date): WorkingDayWindow | null {
    const dow = date.getDay();
    return this.workingDays().find(d => d.day === dow) ?? null;
  }

  protected readonly days = computed<CalendarDayColumn[]>(() => {
    const anchor = this.selectedDate();
    const today = new Date();

    if (this.viewMode() === 'day') {
      return [this.toColumn(anchor, today)];
    }

    const start = new Date(anchor);
    start.setDate(start.getDate() - start.getDay());
    return Array.from({ length: 7 }, (_, i) => {
      const d = new Date(start);
      d.setDate(d.getDate() + i);
      return this.toColumn(d, today);
    });
  });

  protected readonly timelineStartHour = computed(() => {
    const windows = this.days().map(d => this.windowForDay(d.date)).filter((w): w is WorkingDayWindow => w !== null);
    if (windows.length === 0) return 9;
    return Math.floor(Math.min(...windows.map(w => this.hourFraction(w.startTime))));
  });

  protected readonly timelineEndHour = computed(() => {
    const windows = this.days().map(d => this.windowForDay(d.date)).filter((w): w is WorkingDayWindow => w !== null);
    if (windows.length === 0) return 18;
    return Math.ceil(Math.max(...windows.map(w => this.hourFraction(w.endTime))));
  });

  protected readonly hours = computed(() => {
    const list: number[] = [];
    for (let h = this.timelineStartHour(); h <= this.timelineEndHour(); h++) list.push(h);
    return list;
  });

  protected readonly currentTimeTop = computed<number | null>(() => {
    const n = this.now();
    const start = this.timelineStartHour();
    const end = this.timelineEndHour();
    const fraction = n.getHours() + n.getMinutes() / 60;
    if (fraction < start || fraction > end) return null;
    return (fraction - start) * this.hourHeight;
  });

  protected isTodayColumn(date: Date): boolean {
    return date.toDateString() === new Date().toDateString();
  }

  protected appointmentsForDay(date: Date): Appointment[] {
    return this.appointments().filter(a => a.slotStart.toDateString() === date.toDateString());
  }

  protected availabilityForDay(date: Date): AvailableInterval[] {
    return this.availability().filter(i => i.start.toDateString() === date.toDateString());
  }

  protected isWorkingDay(date: Date): boolean {
    return this.windowForDay(date) !== null;
  }

  protected displayedStart(appointment: Appointment): Date {
    return this.liveDragPositions().get(appointment.id)?.start ?? appointment.slotStart;
  }

  protected displayedEnd(appointment: Appointment): Date {
    return this.liveDragPositions().get(appointment.id)?.end ?? appointment.slotEnd;
  }

  protected topFor(date: Date): number {
    return (date.getHours() + date.getMinutes() / 60 - this.timelineStartHour()) * this.hourHeight;
  }

  protected heightFor(start: Date, end: Date): number {
    return ((end.getTime() - start.getTime()) / 3600000) * this.hourHeight;
  }

  protected formatHourLabel(hour: number): string {
    const period = hour >= 12 ? 'PM' : 'AM';
    const displayHour = hour > 12 ? hour - 12 : hour === 0 ? 12 : hour;
    return `${displayHour}:00 ${period}`;
  }

  // ---- Drag / resize ----

  protected onDragStarted(e: { appointment: Appointment; clientY: number }): void {
    this.dragState = {
      appointment: e.appointment,
      originalStart: e.appointment.slotStart,
      originalEnd: e.appointment.slotEnd,
      startClientY: e.clientY,
      mode: 'move'
    };
    this.draggingId.set(e.appointment.id);
    this.attachPointerListeners();
  }

  protected onResizeStarted(e: { appointment: Appointment; clientY: number }): void {
    this.dragState = {
      appointment: e.appointment,
      originalStart: e.appointment.slotStart,
      originalEnd: e.appointment.slotEnd,
      startClientY: e.clientY,
      mode: 'resize'
    };
    this.draggingId.set(e.appointment.id);
    this.attachPointerListeners();
  }

  private attachPointerListeners(): void {
    const onMove = (ev: PointerEvent) => this.handlePointerMove(ev);
    const onUp = (ev: PointerEvent) => {
      window.removeEventListener('pointermove', onMove);
      window.removeEventListener('pointerup', onUp);
      this.handlePointerUp();
    };
    window.addEventListener('pointermove', onMove);
    window.addEventListener('pointerup', onUp);
  }

  private handlePointerMove(event: PointerEvent): void {
    if (!this.dragState) return;
    const deltaY = event.clientY - this.dragState.startClientY;
    const rawMinutes = (deltaY / this.hourHeight) * 60;
    const snappedMinutes = Math.round(rawMinutes / this.snapMinutes) * this.snapMinutes;

    let newStart = this.dragState.originalStart;
    let newEnd = this.dragState.originalEnd;

    if (this.dragState.mode === 'move') {
      newStart = new Date(this.dragState.originalStart.getTime() + snappedMinutes * 60000);
      newEnd = new Date(this.dragState.originalEnd.getTime() + snappedMinutes * 60000);
    } else {
      const minDuration = 15;
      const candidateEnd = new Date(this.dragState.originalEnd.getTime() + snappedMinutes * 60000);
      const minEnd = new Date(this.dragState.originalStart.getTime() + minDuration * 60000);
      newEnd = candidateEnd < minEnd ? minEnd : candidateEnd;
    }

    const map = new Map(this.liveDragPositions());
    map.set(this.dragState.appointment.id, { start: newStart, end: newEnd });
    this.liveDragPositions.set(map);
  }

  private handlePointerUp(): void {
    if (!this.dragState) return;
    const id = this.dragState.appointment.id;
    const live = this.liveDragPositions().get(id);
    const original = { start: this.dragState.originalStart, end: this.dragState.originalEnd };

    this.draggingId.set(null);

    if (live && (live.start.getTime() !== original.start.getTime() || live.end.getTime() !== original.end.getTime())) {
      this.rescheduleRequested.emit({
        appointment: this.dragState.appointment,
        newStart: live.start,
        newEnd: live.end
      });
    }

    const map = new Map(this.liveDragPositions());
    map.delete(id);
    this.liveDragPositions.set(map);
    this.dragState = null;
  }

  // ---- Scroll ----

  private scrollToCurrentHour(): void {
    const container = this.scrollContainer()?.nativeElement;
    if (!container) return;
    const now = new Date();
    const fraction = now.getHours() + now.getMinutes() / 60;
    const start = this.timelineStartHour();
    const offset = Math.max(0, (fraction - start - 1) * this.hourHeight);
    container.scrollTop = offset;
  }

  private hourFraction(time: string): number {
    const [h, m] = time.split(':').map(Number);
    return h + m / 60;
  }

  private toColumn(date: Date, today: Date): CalendarDayColumn {
    return {
      date,
      label: date.toLocaleDateString('en-US', { weekday: 'short' }),
      dayNumber: date.getDate(),
      isToday: date.toDateString() === today.toDateString()
    };
  }
}