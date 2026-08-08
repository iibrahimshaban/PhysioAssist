using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.Translation.Sbg;

public class SbgAnswerTranslationService : IAnswerTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly SbgTranslationChatOptions _options;
    private readonly ILogger<SbgAnswerTranslationService> _logger;

    public SbgAnswerTranslationService(
        HttpClient httpClient,
        IOptions<SbgTranslationChatOptions> options,
        ILogger<SbgAnswerTranslationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<string> TranslateToArabicAsync(string markdownAnswer, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(markdownAnswer))
            return markdownAnswer;

        var payload = new
        {
            model_id = _options.ModelId,
            messages = new object[]
            {
                new { role = "user", content = markdownAnswer }
            },
            system_prompt = TranslationSystemPrompts.TranslateToArabicPrompt,
            max_tokens = 2048
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"{_options.BaseUrl}{_options.ChatPath}", payload, ct);
            response.EnsureSuccessStatusCode();

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            var translated = SbgResponseParser.ExtractContent(rawBody)?.Trim();

            _logger.LogInformation(
                "AnswerTranslation raw model output length: {Length}",
                translated?.Length ?? 0);

            return string.IsNullOrWhiteSpace(translated) ? markdownAnswer : translated;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "AnswerTranslation failed, falling back to original markdown");
            return markdownAnswer; // fallback to original on failure
        }
    }
}
