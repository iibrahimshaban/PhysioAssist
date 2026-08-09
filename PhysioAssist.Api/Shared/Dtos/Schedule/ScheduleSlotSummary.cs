namespace PhysioAssist.Api.Shared.Dtos.Schedule;

public sealed record ScheduleSlotSummary(
    Guid SlotId, 
    DateTimeOffset SlotStart, 
    DateTimeOffset SlotEnd, 
    SlotStatus Status
    );
