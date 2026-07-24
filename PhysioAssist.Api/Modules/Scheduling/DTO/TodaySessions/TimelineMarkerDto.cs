namespace PhysioAssist.Api.Modules.Scheduling.DTO.TodaySessions;

public sealed class TimelineMarkerDto
{
    public Guid SlotId { get; init; }
    public DateTimeOffset SlotStart { get; init; }
    public SlotBoardLane Lane { get; init; }
}

public enum SlotBoardLane { InProgress, UpNext, Completed, Missed }
