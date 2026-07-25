using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;

namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public class CreateSessionPackageRequest
{
    public required Guid PatientId { get; init; }
    public required Guid DoctorId { get; init; }
    public required int TotalSessions { get; init; }
    public required TimeSpan SessionDuration { get; init; }
    public int SessionsPerWeek { get; init; } = 3;
    public int MinimumGapBetweenSessionsDays { get; init; } = 2;
    public PreferredTimeOfDay PreferredTimeOfDay { get; init; } = PreferredTimeOfDay.Unspecified;
    public DaysOfWeekFlags PreferredDays { get; init; } = DaysOfWeekFlags.None;
    public SchedulingPriority Priority { get; init; } = SchedulingPriority.Normal;

    // Set only on the doctor's own "Confirm booking" path, where a slot was already
    // chosen on the same screen. Left null on the receptionist path, so the package
    // is created with ScheduledSessions = 0 and nothing booked yet.
    public SlotCandidateDto? FirstSessionSlot { get; init; }
}
