using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace PhysioAssist.Api.Infrastructure.GroqClient;

public class GroqSessionSummarizationService : ISessionSummarizationService
{
    private readonly HttpClient _httpClient;
    private readonly GroqSummarizationOptions _options;

    public GroqSessionSummarizationService(HttpClient httpClient, IOptions<GroqSummarizationOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<string?> SummarizeSessionAsync(
        string subjective, string? objectiveFindingsJson, string assessment, string plan, CancellationToken ct = default)
    {
        var userContent = $"""
            Subjective: {subjective}
            Objective: {objectiveFindingsJson ?? "(none recorded)"}
            Assessment: {assessment}
            Plan: {plan}
            """;

        var payload = new
        {
            model = _options.ChatModel,
            messages = new object[]
            {
                new { role = "system", content = SessionSummaryPrompts.SystemPrompt },
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
