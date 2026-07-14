namespace PhysioAssist.Api.Shared.Interfaces.Documentation;

public interface IRollupSummarizationService
{
    /// <summary>
    /// Synthesizes multiple sessions' narrative summaries + structured Objective findings
    /// into one case-level summary, shaped by audience/scope/focus. Returns null if the
    /// model failed to produce usable output.
    /// </summary>
    Task<string?> GenerateCaseSummaryAsync(
        List<SessionSummaryInput> sessions,
        SummaryAudience audience,
        SummaryScope? scope,
        List<string>? focusAreas,
        CancellationToken ct = default);
}

public sealed record SessionSummaryInput(string? NarrativeSummary, string? ObjectiveFindingsJson);
