namespace PhysioAssist.Api.Modules.SessionModule.Entities;

public class SessionTranscription : AuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SessionId { get; set; }
    public string RawTranscript { get; set; } = string.Empty;
    public string? EditedTranscript { get; set; }
    public string AudioFileUrl { get; set; } = string.Empty;
    public AudioLanguage Language { get; set; } = AudioLanguage.Mixed;
    public int DurationSeconds { get; set; }
    public TranscriptionStatus Status { get; set; } = TranscriptionStatus.Pending;
    public Session Session { get; set; } = default!;
    public ICollection<SessionTranscriptionChunk> Chunks { get; set; } = [];
}
