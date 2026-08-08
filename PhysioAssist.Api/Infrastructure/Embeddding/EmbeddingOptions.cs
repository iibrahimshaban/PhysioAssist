using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.Embeddding;

public class EmbeddingOptions
{
    public const string SectionName = "GeminiEmbeddingOptions";

    [Required]
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
    public string Token { get; set; } = string.Empty; // separate API key from transcription's
    [Required]
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    public int Dimensions { get; set; } = 1536;
}
