using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient.Options;

public class GitHubModelsChatOptions
{
    public const string SectionName = "GitHubModelsChatOptions";

    [Required]
    public string Endpoint { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    [Required]
    public string ChatModel { get; set; } = "gpt-4o";
}
