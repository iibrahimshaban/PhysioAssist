namespace PhysioAssist.Api.Infrastructure.Chunking.Sbg;

public class SbgChunkingModelOptions
{
    public const string SectionName = "SbgChunkingModelOptions";
    public string BaseUrl { get; set; } = "http://apiaccess.iti.net.eg/api/v1";
    public string ChatPath { get; set; } = "/student/chat";
    public string ModelId { get; set; } = "openai.gpt-oss-120b-1:0";
    public string Token { get; set; } = "";
}
