using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using PhysioAssist.Api.Shared.Options;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace PhysioAssist.Api.Modules.QueryModule.Plugin;

public sealed class WebSearchPlugin(HttpClient httpClient, IOptions<TavilyOptions> options)
{
    private readonly TavilyOptions _options = options.Value;

    [KernelFunction]
    [Description("Searches the web for general clinical/medical reference information — treatment " +
                 "protocols, technique definitions, current guidelines, or recent research. " +
                 "NEVER use this for questions about a specific patient's own records — use " +
                 "SearchSessionChunks for that instead.")]
    public async Task<string> SearchWebAsync(
        [Description("The clinical search query — be specific and include relevant context")] string query,
        CancellationToken ct = default)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "tavily_search",
                arguments = new
                {
                    query,
                    max_results = _options.MaxResults,
                    search_depth = "basic"
                }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(_options.McpUrl, content, ct);

        if (!response.IsSuccessStatusCode)
            return "Web search is currently unavailable.";

        var resultJson = await response.Content.ReadAsStringAsync(ct);

        if (resultJson.StartsWith("event:") || resultJson.StartsWith("data:"))
        {
            var dataLine = resultJson.Split('\n').FirstOrDefault(l => l.StartsWith("data:"));
            if (dataLine is not null)
                resultJson = dataLine["data:".Length..].Trim();
        }

        using var doc = JsonDocument.Parse(resultJson);

        if (doc.RootElement.TryGetProperty("result", out var result) &&
            result.TryGetProperty("content", out var contentArr))
        {
            var text = contentArr.EnumerateArray()
                .Where(r => r.GetProperty("type").GetString() == "text")
                .Select(r => r.GetProperty("text").GetString() ?? string.Empty)
                .FirstOrDefault();
            return text ?? "No results found.";
        }

        return resultJson;
    }
}
