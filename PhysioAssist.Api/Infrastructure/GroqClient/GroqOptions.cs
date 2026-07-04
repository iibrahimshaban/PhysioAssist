using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.GroqClient;

public class GroqOptions
{
    public const string SectionName = "Groq";
    public string ApiKey { get; init; } = string.Empty;
    [Required]
    public string TranscriptionModel { get; init; } = string.Empty;
    [Required]
    public string RefinedModel { get; init; } = string.Empty;
    [Required]
    public string BaseUrl { get; init; } = string.Empty;
}
