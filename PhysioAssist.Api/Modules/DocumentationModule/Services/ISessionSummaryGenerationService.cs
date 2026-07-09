namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public interface ISessionSummaryGenerationService
{
    Task<Result> GenerateAndSaveSummaryAsync(Guid sessionId, CancellationToken ct = default);
}
