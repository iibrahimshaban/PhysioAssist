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
    public SchedulingPriority Priority { get; init; } = SchedulingPriority.Normal;

    public SlotCandidateDto? FirstSessionSlot { get; init; }
}
