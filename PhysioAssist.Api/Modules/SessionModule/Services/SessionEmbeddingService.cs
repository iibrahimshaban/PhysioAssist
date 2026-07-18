using Microsoft.Data.SqlTypes;
using PhysioAssist.Api.Modules.SessionModule.Entities;
using PhysioAssist.Api.Modules.SessionModule.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Chunking;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.SessionModule.Services;

public class SessionEmbeddingService(
    ApplicationDbContext dbContext,
    IEmbeddingService embeddingService,
    ITranscriptChunkingService chunkingService,
    ILogger<SessionEmbeddingService> logger) : ISessionEmbeddingService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IEmbeddingService _embeddingService = embeddingService;
    private readonly ITranscriptChunkingService _chunkingService = chunkingService;
    private readonly ILogger<SessionEmbeddingService> _logger = logger;


    // Refactored to batch generate all embeddings at once instead of per-iteration API calls
    // Build chunk texts first, then single batch call, then map results by index
    public async Task<Result> GenerateAndStoreEmbeddingAsync(
    Guid sessionTranscriptionId, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure(EmbeddingErrors.EmptyText);

        var transcriptionExists = await _dbContext.Set<SessionTranscription>()
            .AnyAsync(t => t.Id == sessionTranscriptionId, ct);

        if (!transcriptionExists)
            return Result.Failure(EmbeddingErrors.TranscriptionNotFound);

        List<ExtractedChunk> extracted;
        try
        {
            extracted = await _chunkingService.ExtractChunksAsync(text, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extraction failed for transcription {TranscriptionId}", sessionTranscriptionId);
            return Result.Failure(EmbeddingErrors.ChunkingFailed);
        }

        if (extracted.Count == 0)
            return Result.Failure(EmbeddingErrors.NoChunks);

        var existingChunks = await _dbContext.Set<SessionTranscriptionChunk>()
            .Where(c => c.SessionTranscriptionId == sessionTranscriptionId)
            .ToListAsync(ct);

        if (existingChunks.Count > 0)
            _dbContext.RemoveRange(existingChunks);

        var newChunks = new List<SessionTranscriptionChunk>(extracted.Count);
        var chunkTexts = new List<string>(extracted.Count);

        for (var i = 0; i < extracted.Count; i++)
        {
            var e = extracted[i];

            // Synthesized sentence — this is what carries meaning into the embedding
            var chunkText = $"{e.Diagnosis}. {e.Recommendations}: {e.RecommendationDetails}." +
                  (e.PatientResponse is not null ? $" {e.PatientResponse}." : "") +
                  (e.Notes is not null ? $" {e.Notes}." : "") +
                  $" {e.NextSessionFocus}.";

            chunkTexts.Add(chunkText);
        }

        List<SqlVector<float>> embeddings;
        try
        {
            embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Embedding failed for transcription {TranscriptionId}", sessionTranscriptionId);
            return Result.Failure(EmbeddingErrors.GenerationFailed);
        }

        for (var i = 0; i < extracted.Count; i++)
        {
            var e = extracted[i];
            newChunks.Add(new SessionTranscriptionChunk
            {
                SessionTranscriptionId = sessionTranscriptionId,
                ChunkIndex = i,
                Recommendations = e.Recommendations,
                RecommendationDetails = e.RecommendationDetails,
                PatientResponse = e.PatientResponse,
                NextSessionFocus = e.NextSessionFocus,
                Diagnosis = e.Diagnosis,
                Notes = e.Notes,
                ChunkText = chunkTexts[i],
                Embedding = embeddings[i],
                CreatedAt = DateTime.UtcNow
            });
        }

        try
        {
            await _dbContext.Set<SessionTranscriptionChunk>().AddRangeAsync(newChunks, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save failed for transcription {TranscriptionId}", sessionTranscriptionId);
            return Result.Failure(EmbeddingErrors.SaveFailed);
        }

        return Result.Success();
    }
}
