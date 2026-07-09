namespace PhysioAssist.Api.Modules.SessionModule.Services;

public interface ISessionEmbeddingService
{
    Task<Result> GenerateAndStoreEmbeddingAsync(Guid sessionTranscriptionId, string text, CancellationToken ct = default);
}
