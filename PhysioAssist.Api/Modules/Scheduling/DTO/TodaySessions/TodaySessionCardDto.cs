namespace PhysioAssist.Api.Modules.Scheduling.DTO.TodaySessions;

public sealed class TodaySessionCardDto
{
    public Guid SlotId { get; init; }
    public Guid? SessionId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public DateTimeOffset SlotStart { get; init; }
    public DateTimeOffset SlotEnd { get; init; }
    public string? Note { get; init; }
    public SlotBoardLane Lane { get; init; }
}
