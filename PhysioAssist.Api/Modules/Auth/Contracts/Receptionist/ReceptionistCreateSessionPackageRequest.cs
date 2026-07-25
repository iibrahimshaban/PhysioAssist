using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.Auth.Contracts.Receptionist;

public class ReceptionistCreateSessionPackageRequest
{
    public required Guid PatientId { get; init; }
    public required int TotalSessions { get; init; }
    public required TimeSpan SessionDuration { get; init; }
    public int SessionsPerWeek { get; init; } = 3;
    public int MinimumGapBetweenSessionsDays { get; init; } = 2;
    public PreferredTimeOfDay PreferredTimeOfDay { get; init; } = PreferredTimeOfDay.Unspecified;
    public DaysOfWeekFlags PreferredDays { get; init; } = DaysOfWeekFlags.None;
    public SchedulingPriority Priority { get; init; } = SchedulingPriority.Normal;
    public SlotCandidateDto? FirstSessionSlot { get; init; }
}
