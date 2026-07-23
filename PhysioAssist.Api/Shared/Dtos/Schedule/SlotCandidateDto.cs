using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;

namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public class SlotCandidateDto
{
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public TimeSpan AvailableDuration { get; init; }
    public TimeSpan RequestedDuration { get; init; }
    public SlotFitType FitType { get; init; }
    public TimeSpan Gap { get; init; }
    public bool IsBeyondPreferredHorizon { get; init; }
    public double Score { get; init; }
}
