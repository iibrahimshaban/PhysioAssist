using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Interfaces.Documentation;
using System.Net.Http.Headers;

namespace PhysioAssist.Api.Infrastructure.GroqClient;

public class GroqRollupSummarizationService : IRollupSummarizationService
{
    private readonly HttpClient _httpClient;
    private readonly GroqRollupSummarizationOptions _options;

    public GroqRollupSummarizationService(HttpClient httpClient, IOptions<GroqRollupSummarizationOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<string?> GenerateCaseSummaryAsync(
        List<SessionSummaryInput> sessions,
        SummaryAudience audience,
        SummaryScope? scope,
        List<string>? focusAreas,
        CancellationToken ct = default)
    {
        if (sessions.Count == 0)
            return null;

        var systemPrompt = RollupSummaryPrompts.BuildSystemPrompt(audience, scope, focusAreas);
        var userContent = RollupSummaryPrompts.BuildUserContent(sessions);

        var payload = new
        {
            model = _options.ChatModel,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            },
            temperature = 0.3
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct);
        var content = result?.Choices?.FirstOrDefault()?.Message?.Content;

        return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
    }

    private sealed record ChatCompletionResponse(List<Choice> Choices);
    private sealed record Choice(ChatMessage Message);
    private sealed record ChatMessage(string Content);
}
