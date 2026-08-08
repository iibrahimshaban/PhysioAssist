using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Modules.QueryModule.Options;

public class SbgQueryAgentChatOptions
{
    public const string SectionName = "SbgQueryAgentChatOptions";
    [Required]
    public string BaseUrl { get; set; } = "http://apiaccess.iti.net.eg/api/v1";
    [Required]
    public string ChatPath { get; set; } = "/student/chat";
    [Required]
    public string ModelId { get; set; } = "openai.gpt-oss-120b-1:0";
    public string Token { get; set; } = "";
}
