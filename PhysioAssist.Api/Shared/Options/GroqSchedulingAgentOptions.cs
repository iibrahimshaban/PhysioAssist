using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Shared.Options;

public class GroqSchedulingAgentOptions
{
    public const string SectionName = "GroqSchedulingAgent";

    public string Token { get; set; } = string.Empty;
    [Required]
    public string ChatModel { get; set; } = string.Empty;
    [Required]
    public string Endpoint { get; set; } = string.Empty;
}
