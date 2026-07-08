using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public class GitHubModelsEmbeddingOptions
{
    public const string SectionName = "GitHubEmbeddingModels";
    [Required]
    public string Endpoint { get; init; } = "https://models.github.ai/inference/embeddings";
    [Required]
    public string EmbeddingModel { get; init; } = "openai/text-embedding-3-small";
    public string Token { get; init; } = default!;
}
