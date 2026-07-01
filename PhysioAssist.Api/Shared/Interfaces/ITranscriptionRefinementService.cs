namespace PhysioAssist.Api.Shared.Interfaces;

public interface ITranscriptionRefinementService
{
    Task<Result<string>> RefineAsync(string rawText, CancellationToken cancellationToken = default);
}
