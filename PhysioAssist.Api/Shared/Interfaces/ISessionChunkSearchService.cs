using PhysioAssist.Api.Shared.Dtos.Chunking;

namespace PhysioAssist.Api.Shared.Interfaces;

public interface ISessionChunkSearchService
{
    Task<Result<List<ChunkSearchResult>>> SearchAsync(
        string englishQuery,
        Guid? patientId = null,
        int topN = 5,
        double maxDistance = 0.4,
        CancellationToken ct = default);
}
