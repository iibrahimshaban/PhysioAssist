using PhysioAssist.Api.Modules.Scheduling.DTO.TodaySessions;
using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class TodaySessionsQueryService(
    ApplicationDbContext context,
    IPatientQueryService patientLookupService,
    ISessionQueryService sessionLookupService) : ITodaySessionsService
{
    private static readonly TimeSpan EgyptOffset = TimeSpan.FromHours(3);

    public async Task<Result<TodaySessionsOverviewDto>> GetTodaySessionsAsync(
        Guid doctorId, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(nowUtc.ToOffset(EgyptOffset).Date);

        var dayStart = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), EgyptOffset);
        var dayEnd = new DateTimeOffset(today.ToDateTime(TimeOnly.MaxValue), EgyptOffset);

        var slots = await context.Set<ScheduleSlot>()
            .Where(s => s.DoctorId == doctorId
                        && s.SlotStart >= dayStart
                        && s.SlotStart <= dayEnd
                        && s.Status != SlotStatus.Cancelled
                        && s.Status != SlotStatus.NoShow)
            .OrderBy(s => s.SlotStart)
            .ToListAsync(cancellationToken);

        if (slots.Count == 0)
            return Result.Success(new TodaySessionsOverviewDto { Date = today });

        var slotIds = slots.Select(s => s.Id).ToList();
        var patientIds = slots.Select(s => s.PatientId).Distinct().ToList();

        var sessionsBySlot = await sessionLookupService.GetSessionsByScheduleSlotIdsAsync(slotIds, cancellationToken);
        var patientsById = await patientLookupService.GetPatientsByIdsAsync(patientIds, cancellationToken);

        var inProgress = new List<TodaySessionCardDto>();
        var upNext = new List<TodaySessionCardDto>();
        var completed = new List<TodaySessionCardDto>();
        var missed = new List<TodaySessionCardDto>();
        var timeline = new List<TimelineMarkerDto>();

        foreach (var slot in slots)
        {
            sessionsBySlot.TryGetValue(slot.Id, out var session);
            patientsById.TryGetValue(slot.PatientId, out var patient);

            var lane = ResolveLane(slot, session?.Status, nowUtc);

            var card = new TodaySessionCardDto
            {
                SlotId = slot.Id,
                SessionId = session?.Id,
                PatientId = slot.PatientId,
                PatientName = patient?.FullName ?? "Unknown Patient",
                SlotStart = slot.SlotStart,
                SlotEnd = slot.SlotEnd,
                Note = patient?.CaseNotes,
                Lane = lane
            };

            timeline.Add(new TimelineMarkerDto { SlotId = slot.Id, SlotStart = slot.SlotStart, Lane = lane });

            (lane switch
            {
                SlotBoardLane.InProgress => inProgress,
                SlotBoardLane.UpNext => upNext,
                SlotBoardLane.Completed => completed,
                _ => missed
            }).Add(card);
        }

        var total = slots.Count;

        return Result.Success(new TodaySessionsOverviewDto
        {
            Date = today,
            TotalToday = total,
            CompletedCount = completed.Count,
            InProgressCount = inProgress.Count,
            UpNextCount = upNext.Count,
            MissedCount = missed.Count,
            PercentDone = total == 0 ? 0 : Math.Round((double)completed.Count / total * 100, 0),
            Timeline = timeline,
            InProgress = inProgress,
            UpNext = upNext,
            Completed = completed,
            Missed = missed
        });
    }

    private static SlotBoardLane ResolveLane(ScheduleSlot slot, SessionStatus? sessionStatus, DateTimeOffset nowUtc)
    {
        if (slot.Status == SlotStatus.Completed)
            return SlotBoardLane.Completed;

        if (sessionStatus == SessionStatus.InProgress)
            return SlotBoardLane.InProgress;

        return slot.SlotEnd < nowUtc ? SlotBoardLane.Missed : SlotBoardLane.UpNext;
    }
}