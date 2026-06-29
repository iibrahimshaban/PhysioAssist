using Microsoft.Data.SqlTypes;

namespace PhysioAssist.Api.Modules.SessionModule.Entities;

public class SessionTranscriptionChunk
{
    public int Id { get; set; }
    public Guid SessionTranscriptionId { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public SqlVector<float> Embedding { get; set; }
    public int? StartOffsetSeconds { get; set; }
    public int? EndOffsetSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public SessionTranscription Transcription { get; set; } = default!;
}
