namespace PhysioAssist.Api.Infrastructure.Documentation.Sbg;

public class SbgDocumentationChatOptions
{
    public const string SectionName = "SbgDocumentationChatOptions";
    public string BaseUrl { get; set; } = "http://apiaccess.iti.net.eg/api/v1";
    public string ChatPath { get; set; } = "/student/chat";
    public string ModelId { get; set; } = "openai.gpt-oss-120b-1:0";
    public string Token { get; set; } = "";
}
