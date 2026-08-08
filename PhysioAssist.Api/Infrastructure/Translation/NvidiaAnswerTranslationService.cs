using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.Translation;

public class NvidiaAnswerTranslationService : IAnswerTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly TranslationChatOptions _options;
    private readonly ILogger<NvidiaAnswerTranslationService> _logger;

    public NvidiaAnswerTranslationService(
        HttpClient httpClient,
        IOptions<TranslationChatOptions> options,
        ILogger<NvidiaAnswerTranslationService> logger)
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
            model = _options.ModelId,
            messages = new object[]
            {
                new { role = "system", content = "en-ar" },
                new { role = "user", content = markdownAnswer }
            },
            max_tokens = _options.MaxTokens
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"{_options.BaseUrl}{_options.ChatPath}", payload, ct);
            response.EnsureSuccessStatusCode();

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            var translated = ExtractContent(rawBody)?.Trim();

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
    private static string? ExtractContent(string rawBody)
    {
        using var doc = JsonDocument.Parse(rawBody);

        if (!doc.RootElement.TryGetProperty("choices", out var choices) ||
            choices.GetArrayLength() == 0)
            return null;

        var firstChoice = choices[0];

        if (!firstChoice.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content))
            return null;

        return content.GetString();
    }
}
