namespace PhysioAssist.Api.Shared.Options;

public sealed class TavilyOptions
{
    public const string SectionName = "Tavily";
    public string McpUrl { get; init; } = string.Empty;
    public int MaxResults { get; init; } = 5;
}