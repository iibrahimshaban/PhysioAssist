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

        // Only real patients go to the cross-module patient lookup now.
        var patientIds = slots
            .Where(s => s.PatientId.HasValue)
            .Select(s => s.PatientId!.Value)
            .Distinct()
            .ToList();

        // Guests are resolved locally — no cross-module call needed, same
        // reasoning as GuestRepository: Guest lives inside Scheduling itself.
        var guestIds = slots
            .Where(s => s.GuestId.HasValue)
            .Select(s => s.GuestId!.Value)
            .Distinct()
            .ToList();

        var sessionsBySlot = await sessionLookupService.GetSessionsByScheduleSlotIdsAsync(slotIds, cancellationToken);

        var patientsById = await patientLookupService.GetPatientsByIdsAsync(patientIds, cancellationToken);

        var guestsById = await context.Set<Guest>()
            .Where(g => guestIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, cancellationToken);

        var inProgress = new List<TodaySessionCardDto>();
        var upNext = new List<TodaySessionCardDto>();
        var completed = new List<TodaySessionCardDto>();
        var missed = new List<TodaySessionCardDto>();
        var timeline = new List<TimelineMarkerDto>();

        foreach (var slot in slots)
        {
            sessionsBySlot.TryGetValue(slot.Id, out var session);

            string displayName;
            string? note = null;

            if (slot.PatientId is { } patientId && patientsById.TryGetValue(patientId, out var patient))
            {
                displayName = patient?.FullName ?? "Unknown Patient";
                note = patient?.CaseNotes;
            }
            else if (slot.GuestId is { } guestId && guestsById.TryGetValue(guestId, out var guest))
            {
                displayName = guest.FullName;
                // Guests carry no case notes — nothing to fall back to here.
            }
            else
            {
                displayName = "Unknown";
            }

            var lane = ResolveLane(slot, session?.Status, nowUtc);

            var card = new TodaySessionCardDto
            {
                SlotId = slot.Id,
                SessionId = session?.Id,
                PatientId = slot.PatientId,
                PatientName = displayName,
                SlotStart = slot.SlotStart,
                SlotEnd = slot.SlotEnd,
                Note = note,
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