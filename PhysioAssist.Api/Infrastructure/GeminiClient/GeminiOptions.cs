using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.GeminiClient;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    [Required] 
    public string ApiKey { get; init; } = default!;
    [Required] 
    public string BaseUrl { get; init; } = default!;
    [Required] 
    public string TranscriptionModel { get; init; } = default!;
}