using Microsoft.Extensions.Options;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient.Options;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient.Prompts;
using PhysioAssist.Api.Shared.Interfaces.Ingestion;
using System.Net.Http.Headers;

namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public class GitHubModelsQueryTranslationService : IQueryTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly GitHubModelsChatOptions _options;

    public GitHubModelsQueryTranslationService(HttpClient httpClient, IOptions<GitHubModelsChatOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }
    public async Task<string> TranslateToEnglishAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return query;

        var payload = new
        {
            model = _options.ChatModel,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompts.TranslateToEnglishPrompt },
                new { role = "user", content = query }
            },
            temperature = 0.0
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct);
        var translated = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

        return string.IsNullOrWhiteSpace(translated) ? query : translated; // fallback to original on empty response
    }

    private sealed record ChatCompletionResponse(List<Choice> Choices);
    private sealed record Choice(ChatMessage Message);
    private sealed record ChatMessage(string Content);
}
