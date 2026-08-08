using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Interfaces.Documentation;
using System.Net.Http.Headers;

namespace PhysioAssist.Api.Infrastructure.Documentation.Sbg;

public class SbgSessionSummarizationService : ISessionSummarizationService
{
    private readonly HttpClient _httpClient;
    private readonly SbgDocumentationChatOptions _options;

    public SbgSessionSummarizationService(HttpClient httpClient, IOptions<SbgDocumentationChatOptions> options)
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
            model_id = _options.ModelId,
            messages = new object[] { new { role = "user", content = userContent } },
            system_prompt = DocumnetationSystemPrompts.SessionSummaryPrompt,
            max_tokens = 1024
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"{_options.BaseUrl}{_options.ChatPath}", payload, ct);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(ct);
        return SbgResponseParser.ExtractContent(raw)?.Trim();
    }
}
