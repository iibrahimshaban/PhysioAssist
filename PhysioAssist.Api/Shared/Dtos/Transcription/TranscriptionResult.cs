namespace PhysioAssist.Api.Shared.Dtos.Transcription;

public record TranscriptionResult(
    string RawText,
    string RefinedText,
    AudioLanguage DetectedLanguage,
    double? DurationSeconds
    );

