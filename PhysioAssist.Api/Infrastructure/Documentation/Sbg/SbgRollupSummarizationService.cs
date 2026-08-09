using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Interfaces.Documentation;
using System.Net.Http.Headers;

namespace PhysioAssist.Api.Infrastructure.Documentation.Sbg;

public class SbgRollupSummarizationService : IRollupSummarizationService
{
    private readonly HttpClient _httpClient;
    private readonly SbgDocumentationChatOptions _options;

    public SbgRollupSummarizationService(HttpClient httpClient, IOptions<SbgDocumentationChatOptions> options)
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

        var systemPrompt = DocumnetationSystemPrompts.BuildRollUpSystemPrompt(audience, scope, focusAreas);
        var userContent = DocumnetationSystemPrompts.BuildRollUpUserContent(sessions);

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
        var content = SbgResponseParser.ExtractContent(raw);

        return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
    }
}
