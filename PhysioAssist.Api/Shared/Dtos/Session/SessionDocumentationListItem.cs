namespace PhysioAssist.Api.Shared.Dtos.Session;

public sealed record SessionDocumentationListItem(
    Guid SessionId, Guid? ScheduleSlotId, SessionStatus Status,
    string? SummaryText, DateTime? SummaryGeneratedAt);
