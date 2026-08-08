using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Interfaces.Documentation;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PhysioAssist.Api.Infrastructure.Documentation.Sbg;

public class SbgDocumentationExtractionService : IDocumentationExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly SbgDocumentationChatOptions _options;

    public SbgDocumentationExtractionService(HttpClient httpClient, IOptions<SbgDocumentationChatOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<NarrativeDraftResult?> DraftNarrativeAsync(string transcriptText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(transcriptText))
            return null;

        var content = await CompleteAsync(
            DocumnetationSystemPrompts.BuildNarrativeDraftSystemPrompt(), transcriptText, ct);

        if (string.IsNullOrWhiteSpace(content))
            return null;

        content = CleanJsonFence(content);

        try
        {
            return JsonSerializer.Deserialize<NarrativeDraftResult>(
                content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<string?> ExtractObjectiveFindingsAsync(
        string transcriptText, JsonArray effectiveFields, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(transcriptText) || effectiveFields.Count == 0)
            return null;

        var systemPrompt = DocumnetationSystemPrompts.BuildDocumentationSystemPrompt(effectiveFields);
        var content = await CompleteAsync(systemPrompt, transcriptText, ct);

        if (string.IsNullOrWhiteSpace(content))
            return null;

        content = CleanJsonFence(content);

        try
        {
            using var _ = JsonDocument.Parse(content);
            return content;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<string?> CompleteAsync(string systemPrompt, string userContent, CancellationToken ct)
    {
        var payload = new
        {
            model_id = _options.ModelId,
            messages = new object[] { new { role = "user", content = userContent } },
            system_prompt = systemPrompt,
            max_tokens = 2048
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"{_options.BaseUrl}{_options.ChatPath}", payload, ct);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(ct);
        return SbgResponseParser.ExtractContent(raw);
    }

    private static string CleanJsonFence(string content) =>
        content.Trim().Trim('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();
}
