namespace PhysioAssist.Api.Shared.Dtos.Session;

public sealed record SessionSummaryItem(
    Guid SessionId,
    string? NarrativeSummary,
    DateTime? SummaryGeneratedAt);
