using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;

namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public class ConvertPlanToPackageRequest
{
    public int? SessionsPerWeek { get; init; }
    public int? MinimumGapBetweenSessionsDays { get; init; }
    public SchedulingPriority? Priority { get; init; }
}
