using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Errors;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.Interfaces.Documentation;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.GroqClient;

public class GroqPatientSummaryService : IPatientSummaryAiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly GroqPatientSummaryOptions _options;
    private readonly ILogger<GroqPatientSummaryService> _logger;

    public GroqPatientSummaryService(HttpClient httpClient, IOptions<GroqPatientSummaryOptions> options, ILogger<GroqPatientSummaryService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<Result<string>> GeneratePatientFriendlySummaryAsync(string clinicalReportText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clinicalReportText))
            return Result.Failure<string>(PatientSummaryErrors.EmptyInput);

        var payload = new
        {
            model = _options.ChatModel,
            messages = new object[]
            {
                new { role = "system", content = PatientSummaryPrompts.SystemPrompt },
                new { role = "user", content = clinicalReportText }
            },
            temperature = 0.4,
            reasoning_effort = "low",
            include_reasoning = false
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, payload, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Groq raw response: {Body}", rawBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Groq patient summary call failed. Status: {Status}, Body: {Body}", response.StatusCode, rawBody);
                return Result.Failure<string>(PatientSummaryErrors.GenerationFailed);
            }

            var result = JsonSerializer.Deserialize<ChatCompletionResponse>(rawBody, JsonOptions);
            var content = result?.Choices?.FirstOrDefault()?.Message?.Content;

            return string.IsNullOrWhiteSpace(content)
                ? Result.Failure<string>(PatientSummaryErrors.GenerationFailed)
                : Result.Success(content.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Groq for patient summary");
            return Result.Failure<string>(PatientSummaryErrors.GenerationFailed);
        }
    }

    private sealed record ChatCompletionResponse(List<Choice> Choices);
    private sealed record Choice(ChatMessage Message);
    private sealed record ChatMessage(string Content);
}