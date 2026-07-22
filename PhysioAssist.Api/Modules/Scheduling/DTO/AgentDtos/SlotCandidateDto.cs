namespace PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;

public class SlotCandidateDto
{
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public TimeSpan AvailableDuration { get; init; }
    public TimeSpan RequestedDuration { get; init; }
    public SlotFitType FitType { get; init; }

    /// <summary>
    /// For Exact: zero (duration matches exactly, whole slot is booked).
    /// For LongerThanRequested: the surplus — how much time stays free after the
    /// requested duration is booked at the start of this interval.
    /// For ShorterThanRequested: the shortfall — how much less than requested this
    /// slot offers (the entire slot is booked; nothing is left free).
    /// </summary>
    public TimeSpan Gap { get; init; }

    /// <summary>
    /// True when this slot's date falls beyond the doctor's MaxDaysOutForExactMatch —
    /// informational flag for the agent/receptionist, not an exclusion.
    /// </summary>
    public bool IsBeyondPreferredHorizon { get; init; }

    /// <summary>0-1 ranking score. Exact fits score highest; near-misses and far-out slots score lower.</summary>
    public double Score { get; init; }
}
