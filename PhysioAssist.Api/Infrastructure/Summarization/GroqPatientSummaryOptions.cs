using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.Summarization;

public class GroqPatientSummaryOptions
{
    public const string SectionName = "GroqPatientSummary";

    [Required]
    public string Endpoint { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
    public string Token { get; set; } = string.Empty;
    [Required]
    public string ChatModel { get; set; } = "openai/gpt-oss-20b";
}
