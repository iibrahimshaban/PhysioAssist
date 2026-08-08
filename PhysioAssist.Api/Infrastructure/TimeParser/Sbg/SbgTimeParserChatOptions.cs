namespace PhysioAssist.Api.Infrastructure.TimeParser.Sbg;

public class SbgTimeParserChatOptions
{
    public const string SectionName = "SbgTimeParserChatOptions";
    public string BaseUrl { get; set; } = "http://apiaccess.iti.net.eg/api/v1";
    public string ChatPath { get; set; } = "/student/chat";
    public string ModelId { get; set; } = "openai.gpt-oss-20b-1:0";
    public string Token { get; set; } = "";
}
