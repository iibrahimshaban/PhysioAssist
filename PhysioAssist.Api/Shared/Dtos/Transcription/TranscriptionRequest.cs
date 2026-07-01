namespace PhysioAssist.Api.Shared.Dtos.Transcription;

public record TranscriptionRequest(
    Stream AudioStream,
    string FileName,
    string? LanguageHint,
    string? Prompt
    );

