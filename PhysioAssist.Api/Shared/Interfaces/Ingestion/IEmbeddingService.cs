using Microsoft.Data.SqlTypes;

namespace PhysioAssist.Api.Shared.Interfaces.Ingestion;

public interface IEmbeddingService
{
    Task<SqlVector<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}
