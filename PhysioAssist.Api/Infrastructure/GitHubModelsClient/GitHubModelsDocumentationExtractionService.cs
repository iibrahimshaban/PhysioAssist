using Microsoft.Extensions.Options;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient.Options;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient.Prompts;
using PhysioAssist.Api.Shared.Interfaces.Documentation;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public class GitHubModelsDocumentationExtractionService : IDocumentationExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly GitHubModelsDocumentationOptions _options;

    public GitHubModelsDocumentationExtractionService(HttpClient httpClient, IOptions<GitHubModelsDocumentationOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<string?> ExtractObjectiveFindingsAsync(string transcriptText, JsonArray effectiveFields, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(transcriptText) || effectiveFields.Count == 0)
            return null;

        var systemPrompt = SystemPrompts.BuildDocumentationSystemPrompt(effectiveFields);

        var payload = new
        {
            model = _options.ChatModel,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = transcriptText }
            },
            temperature = 0.0
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct);
        var content = result?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
            return null;

        // Same defensive fence-stripping as GitHubModelsChunkingService — GPT models
        // sometimes wrap JSON in ```json fences despite instructions.
        content = content.Trim().Trim('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();

        try
        {
            // Validate it's real JSON before returning it — don't hand garbage back to the caller.
            using var _ = JsonDocument.Parse(content);
            return content;
        }
        catch (JsonException)
        {
            // Malformed JSON from the model — don't crash the pipeline, return null
            // and let the caller's Result/Error handling take it from there.
            return null;
        }
    }

    private sealed record ChatCompletionResponse(List<Choice> Choices);
    private sealed record Choice(ChatMessage Message);
    private sealed record ChatMessage(string Content);
}
