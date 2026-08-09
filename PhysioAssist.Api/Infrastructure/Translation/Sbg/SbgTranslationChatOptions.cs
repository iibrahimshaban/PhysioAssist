namespace PhysioAssist.Api.Infrastructure.Translation.Sbg;

public class SbgTranslationChatOptions
{
    public const string SectionName = "SbgTranslationChatOptions";
    public string BaseUrl { get; set; } = "http://apiaccess.iti.net.eg/api/v1";
    public string ChatPath { get; set; } = "/student/chat";
    public string ModelId { get; set; } = "deepseek.v3.2";
    public string Token { get; set; } = "";
}
