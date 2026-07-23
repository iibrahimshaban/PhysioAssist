namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public class SessionBookingRoundDto
{
    public required Guid PackageId { get; init; }

    // 1-based ordinal of the session this round is for (ScheduledSessions + 1)
    public required int SessionNumber { get; init; }
    public required int TotalSessions { get; init; }
    public required int RemainingSessions { get; init; }

    // How many sessions the plan wants booked this week, and how many of those
    // are already confirmed — lets the receptionist UI know when to stop asking
    // for "one more this week" and roll over to next week's batch.
    public required int WeeklyTargetCount { get; init; }
    public required int ScheduledThisWeek { get; init; }

    public required DateOnly WeekStart { get; init; }
    public required DateOnly WeekEnd { get; init; }

    // True once ScheduledThisWeek reaches WeeklyTargetCount — no candidates are
    // generated in this case, Candidates will be empty.
    public required bool WeeklyQuotaMet { get; init; }

    // True when the quota isn't met yet, but there's no room left between the
    // gap-derived earliest date and the end of the current working-week cycle
    // (e.g. gap too large relative to days remaining, or the doctor has no
    // working day at all inside this cycle). Candidates will be empty in this
    // case too.
    public required bool NoRoomLeftThisWeek { get; init; }

    public required IReadOnlyList<SlotCandidateDto> Candidates { get; init; }
    public required string PatientFreeTimeText { get; init; }
}
