using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public class GitHubModelsDocumentationOptions
{
    public const string SectionName = "GitHubModelsDocumentationOptions";

    [Required]
    public string Endpoint { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    [Required]
    public string ChatModel { get; set; } = "gpt-4o-mini";
}
