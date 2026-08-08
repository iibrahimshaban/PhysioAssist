using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Modules.QueryModule.Options;

public class QueryAgentChatOptions
{
    public const string SectionName = "QueryAgentChatOptions";

    [Required]
    public string Endpoint { get; set; } = "https://integrate.api.nvidia.com/v1";
    public string Token { get; set; } = string.Empty; // NVIDIA Build API key from build.nvidia.com
    [Required]
    public string ChatModel { get; set; } = "nvidia/llama-3.3-nemotron-super-49b-v1.5";
}
