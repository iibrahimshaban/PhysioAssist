namespace PhysioAssist.Api.Shared.Dtos.Session;

public enum NextSessionBookingState
{
    NotApplicable,
    CanBookNext,
    LastSessionDecisionNeeded
}

public sealed class NextSessionBookingContextDto
{
    public NextSessionBookingState State { get; init; }
    public Guid? PackageId { get; init; }
    public DateTimeOffset? NextScheduledSlotStart { get; init; }
}
