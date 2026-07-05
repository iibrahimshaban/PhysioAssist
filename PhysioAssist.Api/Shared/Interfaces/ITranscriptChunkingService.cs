using PhysioAssist.Api.Shared.Dtos.Chunking;

namespace PhysioAssist.Api.Shared.Interfaces;

public interface ITranscriptChunkingService
{
    Task<List<ExtractedChunk>> ExtractChunksAsync(string text, CancellationToken ct = default);

}
