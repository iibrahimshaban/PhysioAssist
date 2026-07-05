using Microsoft.Data.SqlTypes;
using PhysioAssist.Api.Modules.SessionModule.Entities;
using PhysioAssist.Api.Modules.SessionModule.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Chunking;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.SessionModule.Services;

public class SessionChunkSearchService : ISessionChunkSearchService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<SessionChunkSearchService> _logger;

    public SessionChunkSearchService(
        ApplicationDbContext dbContext,
        IEmbeddingService embeddingService,
        ILogger<SessionChunkSearchService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<Result<List<ChunkSearchResult>>> SearchAsync(
        string englishQuery,
        Guid? patientId = null,
        int topN = 5,
        double maxDistance = 0.4,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(englishQuery))
            return Result.Failure<List<ChunkSearchResult>>(SearchErrors.EmptyQuery);

        SqlVector<float> queryEmbedding;
        try
        {
            queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(englishQuery, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query embedding failed for '{Query}'", englishQuery);
            return Result.Failure<List<ChunkSearchResult>>(SearchErrors.EmbeddingFailed);
        }

        try
        {
            var results = await _dbContext.SessionTranscriptionChunks
                .Select(c => new
                {
                    Chunk = c,
                    Distance = EF.Functions.VectorDistance("cosine", c.Embedding, queryEmbedding)
                })
                .Where(x => patientId == null || x.Chunk.Transcription.Session.PatientId == patientId)
                .OrderBy(x => x.Distance)
                .Take(topN)
                .Select(x => new ChunkSearchResult(
                    x.Chunk.Id,
                    x.Chunk.SessionTranscriptionId,
                    x.Chunk.Transcription.SessionId,
                    x.Chunk.Transcription.Session.PatientId,
                    x.Chunk.Transcription.Session.DoctorId,
                    x.Chunk.Recommendations,
                    x.Chunk.RecommendationDetails,
                    x.Chunk.PatientResponse,
                    x.Chunk.NextSessionFocus,
                    x.Chunk.Diagnosis,
                    x.Chunk.Notes,
                    x.Distance))
                .ToListAsync(ct);

            return Result.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector search failed for '{Query}'", englishQuery);
            return Result.Failure<List<ChunkSearchResult>>(SearchErrors.SearchFailed);
        }
    }
}
