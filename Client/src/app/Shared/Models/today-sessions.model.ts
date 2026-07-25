export enum SlotBoardLane {
  InProgress = 0,
  UpNext = 1,
  Completed = 2,
  Missed = 3,
}

export interface TimelineMarkerDto {
  slotId: string;
  slotStart: string;
  lane: SlotBoardLane;
}

export interface TodaySessionCardDto {
  slotId: string;
  sessionId: string | null;
  patientId: string;
  patientName: string;
  slotStart: string;
  slotEnd: string;
  note: string | null;
  lane: SlotBoardLane;
}

export interface TodaySessionsOverviewDto {
  date: string;
  totalToday: number;
  completedCount: number;
  inProgressCount: number;
  upNextCount: number;
  missedCount: number;
  percentDone: number;
  timeline: TimelineMarkerDto[];
  inProgress: TodaySessionCardDto[];
  upNext: TodaySessionCardDto[];
  completed: TodaySessionCardDto[];
  missed: TodaySessionCardDto[];
}