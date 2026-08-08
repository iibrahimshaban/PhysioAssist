using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Dtos.Patient;
using PhysioAssist.Api.Shared.Interfaces.Scheduling;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.TimeParser.Sbg;

public class SbgTimePreferenceParser : IPatientTimePreferenceParser
{
    private readonly HttpClient _httpClient;
    private readonly SbgTimeParserChatOptions _options;
    private readonly ILogger<SbgTimePreferenceParser> _logger;

    private static readonly TimeSpan EgyptOffset = TimeSpan.FromHours(3);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public SbgTimePreferenceParser(
        HttpClient httpClient,
        IOptions<SbgTimeParserChatOptions> options,
        ILogger<SbgTimePreferenceParser> logger)
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
            model_id = _options.ModelId,
            messages = new object[]
            {
                new { role = "user", content = englishFreeText }
            },
            system_prompt = TimePreferenceExtractionPrompts.BuildSystemPrompt(todayInEgypt),
            max_tokens = 2048
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"{_options.BaseUrl}{_options.ChatPath}", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var raw = SbgResponseParser.ExtractContent(rawBody)?.Trim();

            _logger.LogInformation(
                "TimePreferenceParser input: {Input} | raw model output: {Raw}",
                englishFreeText,
                raw);

            if (string.IsNullOrWhiteSpace(raw))
                return Result.Success(new PatientTimePreferenceDto());

            var parsed = JsonSerializer.Deserialize<RawTimePreference>(raw, JsonOptions);

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

        var groups = new List<PatientPreferredTimeGroupDto>();
        if (raw.Groups is { Count: > 0 })
        {
            foreach (var g in raw.Groups)
            {
                var weekdays = DaysOfWeekFlags.None;
                if (g.Weekdays is { Count: > 0 })
                {
                    foreach (var day in g.Weekdays)
                        if (Enum.TryParse<DaysOfWeekFlags>(day, ignoreCase: true, out var flag))
                            weekdays |= flag;
                }

                groups.Add(new PatientPreferredTimeGroupDto
                {
                    Weekdays = weekdays,
                    TimeFrom = TimeOnly.TryParse(g.TimeFrom, out var from) ? from : null,
                    TimeTo = TimeOnly.TryParse(g.TimeTo, out var to) ? to : null
                });
            }
        }

        return new PatientTimePreferenceDto
        {
            DayToken = dayToken,
            ExplicitDate = DateOnly.TryParse(raw.ExplicitDate, out var explicitDate) ? explicitDate : null,
            Groups = groups
        };
    }

    private sealed record RawTimePreference(
        string? DayToken,
        string? ExplicitDate,
        List<RawTimePreferenceGroup>? Groups
    );
    private sealed record RawTimePreferenceGroup(
        List<string>? Weekdays,
        string? TimeFrom,
        string? TimeTo
    );
}
