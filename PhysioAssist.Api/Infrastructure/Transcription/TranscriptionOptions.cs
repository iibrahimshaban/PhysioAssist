using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.Transcription;

public sealed class TranscriptionOptions
{
    public const string SectionName = "Gemini";
    public string ApiKey { get; init; } = default!;
    [Required]
    public string BaseUrl { get; init; } = default!;
    [Required]
    public string TranscriptionModel { get; init; } = default!;
}
