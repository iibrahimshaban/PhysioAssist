using Microsoft.Data.SqlTypes;

namespace PhysioAssist.Api.Shared.Interfaces;

public interface IEmbeddingService
{
    Task<SqlVector<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}
