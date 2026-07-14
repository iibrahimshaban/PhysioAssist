namespace PhysioAssist.Api.Shared.Interfaces.Documentation;

public interface ISessionSummarizationService
{
    Task<string?> SummarizeSessionAsync(
       string subjective, string? objectiveFindingsJson, string assessment, string plan, CancellationToken ct = default);
}
