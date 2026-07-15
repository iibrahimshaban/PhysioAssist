using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.GroqClient;

public class GroqSummarizationOptions
{
    public const string SectionName = "GroqSummarizationOptions";

    [Required]
    public string Endpoint { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
    public string Token { get; set; } = string.Empty;
    [Required]
    public string ChatModel { get; set; } = "openai/gpt-oss-20b";
}
