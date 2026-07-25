namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public class CreateSessionPackageResult
{
    public required Guid PackageId { get; init; }
    public required int ScheduledSessions { get; init; }
    public ScheduleSlotDto? FirstSessionSlot { get; init; }
}
