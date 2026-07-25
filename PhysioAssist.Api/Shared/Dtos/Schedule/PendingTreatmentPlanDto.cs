using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;

namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public class PendingTreatmentPlanDto
{
    public required Guid TreatmentPlanId { get; init; }
    public required Guid ReportId { get; init; }
    public required int TotalSessions { get; init; }
    public required int SessionDurationMinutes { get; init; }
    public required int SessionsPerWeek { get; init; }
    public required int MinimumGapBetweenSessionsDays { get; init; }
    public required PreferredTimeOfDay PreferredTimeOfDay { get; init; }
    public required DaysOfWeekFlags PreferredDays { get; init; }
    public required SchedulingPriority Priority { get; init; }
}
