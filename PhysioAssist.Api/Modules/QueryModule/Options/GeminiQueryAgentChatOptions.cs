using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Modules.QueryModule.Options;

public class GeminiQueryAgentChatOptions
{
    public const string SectionName = "GeminiQueryAgentChatOptions";

    [Required]
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/openai/";

    public string Token { get; set; } = string.Empty; // Gemini API key from aistudio.google.com

    [Required]
    public string ChatModel { get; set; } = "gemini-3.6-flash";
}
