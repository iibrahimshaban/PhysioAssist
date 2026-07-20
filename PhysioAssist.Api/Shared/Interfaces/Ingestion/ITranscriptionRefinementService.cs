namespace PhysioAssist.Api.Shared.Interfaces.Ingestion;

public interface ITranscriptionRefinementService
{
    Task<Result<string>> RefineAsync(string rawText, CancellationToken cancellationToken = default);
}
