using Microsoft.Data.SqlTypes;

namespace PhysioAssist.Api.Modules.SessionModule.Entities;

public class SessionTranscriptionChunk
{
    public int Id { get; set; }
    public Guid SessionTranscriptionId { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public SqlVector<float> Embedding { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public string RecommendationDetails { get; set; } = string.Empty;
    public string? PatientResponse { get; set; }
    public string NextSessionFocus { get; set; } = string.Empty;     
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public SessionTranscription Transcription { get; set; } = default!;
}
