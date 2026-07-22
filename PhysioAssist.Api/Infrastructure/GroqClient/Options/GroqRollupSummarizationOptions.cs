using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.GroqClient.Options;

public class GroqRollupSummarizationOptions
{
    public const string SectionName = "GroqRollupSummarizationOptions";

    [Required]
    public string Endpoint { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
    public string Token { get; set; } = string.Empty;
    [Required]
    public string ChatModel { get; set; } = "openai/gpt-oss-120b";
}
