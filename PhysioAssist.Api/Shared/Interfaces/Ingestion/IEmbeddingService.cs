using Microsoft.Data.SqlTypes;

namespace PhysioAssist.Api.Shared.Interfaces.Ingestion;

public interface IEmbeddingService
{
    Task<SqlVector<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default);

    // Added batch embedding method to reduce HTTP overhead when processing multiple chunks
    Task<List<SqlVector<float>>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken ct = default);
}
