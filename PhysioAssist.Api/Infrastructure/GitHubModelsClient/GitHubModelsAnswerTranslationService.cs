using Microsoft.Extensions.Options;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient.Options;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient.Prompts;
using System.Net.Http.Headers;

namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public class GitHubModelsAnswerTranslationService : IAnswerTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly GitHubModelsChatOptions _options;

    public GitHubModelsAnswerTranslationService(HttpClient httpClient, IOptions<GitHubModelsChatOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<string> TranslateToArabicAsync(string markdownAnswer, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(markdownAnswer))
            return markdownAnswer;

        var payload = new
        {
            model = _options.ChatModel,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompts.TranslateToArabicPrompt },
                new { role = "user", content = markdownAnswer }
            },
            temperature = 0.0
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct);
        var translated = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

        return string.IsNullOrWhiteSpace(translated) ? markdownAnswer : translated;
    }

    private sealed record ChatCompletionResponse(List<Choice> Choices);
    private sealed record Choice(ChatMessage Message);
    private sealed record ChatMessage(string Content);
}
