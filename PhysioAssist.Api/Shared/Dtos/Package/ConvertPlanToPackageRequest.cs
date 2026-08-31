namespace PhysioAssist.Api.Shared.Dtos.Package;

public class ConvertPlanToPackageRequest
{
    public int? SessionsPerWeek { get; init; }
    public int? MinimumGapBetweenSessionsDays { get; init; }
    public SchedulingPriority? Priority { get; init; }
}
