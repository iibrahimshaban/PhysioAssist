using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.Chunking;

public class ChunkingModelOptions
{
    public const string SectionName = "ChunkingModelOptions";

    [Required]
    public string Endpoint { get; set; } = "https://integrate.api.nvidia.com/v1/chat/completions";
    public string Token { get; set; } = string.Empty; // reuse your NVIDIA Build key
    [Required]
    public string ChatModel { get; set; } = "nvidia/llama-3.3-nemotron-super-49b-v1.5";
}
