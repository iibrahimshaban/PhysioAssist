using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;

namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public class CreatePackageWithFirstBookingRequest
{
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }

    // --- Mandatory treatment-plan fields ---
    public int TotalSessions { get; init; }
    public TimeSpan SessionDuration { get; init; }

    // --- Sourced from Initial Report / Initial Report's own defaults ---
    public int SessionsPerWeek { get; init; } = 3;
    public int MinimumGapBetweenSessionsDays { get; init; } = 2;

    // --- Scheduling-owned optional preferences ---
    public PreferredTimeOfDay PreferredTimeOfDay { get; init; } = PreferredTimeOfDay.Unspecified;
    public DaysOfWeekFlags PreferredDays { get; init; } = DaysOfWeekFlags.None;
    public SchedulingPriority Priority { get; init; } = SchedulingPriority.Normal;

    // --- The already-chosen first slot (from a SlotCandidateDto the caller selected) ---
    public DateTimeOffset SlotStart { get; init; }
    public DateTimeOffset SlotEnd { get; init; }
}
