using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.TimeParser;

public class TimeParserChatOptions
{
    public const string SectionName = "TimeParserChatOptions";

    [Required]
    public string Endpoint { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    [Required]
    public string ChatModel { get; set; } = "openai/gpt-oss-20b";
}
