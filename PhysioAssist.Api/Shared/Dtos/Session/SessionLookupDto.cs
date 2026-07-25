namespace PhysioAssist.Api.Shared.Dtos.Session;

public sealed record SessionLookupDto(
    Guid Id,
    Guid ScheduleSlotId,
    SessionStatus Status
);
