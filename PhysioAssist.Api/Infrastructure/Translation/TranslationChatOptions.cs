namespace PhysioAssist.Api.Infrastructure.Translation;

public class TranslationChatOptions
{
    public const string SectionName = "TranslationChatOptions";

    public string BaseUrl { get; set; } = "https://integrate.api.nvidia.com/v1";
    public string ChatPath { get; set; } = "/chat/completions";
    public string ModelId { get; set; } = "nvidia/riva-translate-4b-instruct-v2";
    public string Token { get; set; } = "";
    public int MaxTokens { get; set; } = 2048;
}
