using Microsoft.Extensions.Options;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient.Options;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient.Prompts;
using PhysioAssist.Api.Shared.Interfaces.Scheduling;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public class GitHubModelsTimePreferenceParser : IPatientTimePreferenceParser
{
    private readonly HttpClient _httpClient;
    private readonly GitHubModelsChatOptions _options;
    private readonly ILogger<GitHubModelsTimePreferenceParser> _logger;

    // Same convention as DoctorScheduleRecommendationService — today's date in Egypt
    // must come from real system time, never from the model.
    private static readonly TimeSpan EgyptOffset = TimeSpan.FromHours(3);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GitHubModelsTimePreferenceParser(
        HttpClient httpClient,
        IOptions<GitHubModelsChatOptions> options,
        ILogger<GitHubModelsTimePreferenceParser> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<Result<PatientTimePreferenceDto>> ParseAsync(string englishFreeText, CancellationToken cancellationToken = default)
    {

        if (string.IsNullOrWhiteSpace(englishFreeText))
            return Result.Success(new PatientTimePreferenceDto());

        var todayInEgypt = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(EgyptOffset).Date);

        var payload = new
        {
            model = _options.ChatModel,
            messages = new object[]
            {
                    new { role = "system", content = TimePreferenceExtractionPrompts.BuildSystemPrompt(todayInEgypt) },
                    new { role = "user", content = englishFreeText }
            },
            temperature = 0.0
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);
            var raw = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

            _logger.LogInformation(
                "TimePreferenceParser input: {Input} | raw model output: {Raw}",
                englishFreeText,
                raw);

            if (string.IsNullOrWhiteSpace(raw))
                return Result.Success(new PatientTimePreferenceDto());

            var parsed = JsonSerializer.Deserialize<RawTimePreference>(raw, JsonOptions);

            // TEMP DIAGNOSTIC LOGGING — shows the deserialized raw fields before they're
            // converted to TimeOnly/DateOnly, to isolate parsing bugs from model-output bugs.
            _logger.LogInformation(
                "TimePreferenceParser deserialized: DayToken={DayToken}, ExplicitDate={ExplicitDate}, TimeFrom={TimeFrom}, TimeTo={TimeTo}",
                parsed?.DayToken,
                parsed?.ExplicitDate,
                parsed?.TimeFrom,
                parsed?.TimeTo);

            return Result.Success(parsed is null ? new PatientTimePreferenceDto() : MapToDto(parsed));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "TimePreferenceParser failed to parse model response for input: {Input}", englishFreeText);
            return Result.Success(new PatientTimePreferenceDto());
        }
    }

    private static PatientTimePreferenceDto MapToDto(RawTimePreference raw)
    {
        Enum.TryParse<RelativeDayToken>(raw.DayToken, ignoreCase: true, out var dayToken);

        var weekdays = DaysOfWeekFlags.None;
        if (raw.Weekdays is { Count: > 0 })
        {
            foreach (var day in raw.Weekdays)
            {
                if (Enum.TryParse<DaysOfWeekFlags>(day, ignoreCase: true, out var flag))
                    weekdays |= flag;
            }
        }

        return new PatientTimePreferenceDto
        {
            DayToken = dayToken,
            PreferredWeekdays = weekdays,
            ExplicitDate = DateOnly.TryParse(raw.ExplicitDate, out var explicitDate) ? explicitDate : null,
            PreferredTimeFrom = TimeOnly.TryParse(raw.TimeFrom, out var from) ? from : null,
            PreferredTimeTo = TimeOnly.TryParse(raw.TimeTo, out var to) ? to : null
        };
    }

    private sealed record ChatCompletionResponse(List<Choice> Choices);
    private sealed record Choice(ChatMessage Message);
    private sealed record ChatMessage(string Content);
    private sealed record RawTimePreference(
    string? DayToken,
    List<string>? Weekdays,
    string? ExplicitDate,
    string? TimeFrom,
    string? TimeTo
        );
}