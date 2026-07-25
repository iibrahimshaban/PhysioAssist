using Microsoft.Extensions.Options;
using PhysioAssist.Api.Infrastructure.GeminiClient;
using PhysioAssist.Api.Infrastructure.GroqClient.Options;
using PhysioAssist.Api.Shared.Interfaces.Ingestion;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.GroqClient;

public class GroqRefinementClient : ITranscriptionRefinementService
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;

    public GroqRefinementClient(HttpClient httpClient, IOptions<GroqOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    public async Task<Result<string>> RefineAsync(
        string rawText,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _options.RefinedModel,  
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "system", content = TranscriptionPrompts.MedicalRefinement },
                new { role = "user",   content = rawText }
            }
        };

        var body = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = body
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        // Debugging snippet

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<string>(
                new Error("Refinement.NetworkError", ex.Message, StatusCodes.Status503ServiceUnavailable));
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<string>(
                new Error("Refinement.ProviderError",
                    $"Groq chat returned {(int)response.StatusCode}: {err}",
                    (int)response.StatusCode));
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(json);
        var refined = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(refined))
            return Result.Failure<string>(
                new Error("Refinement.EmptyResponse", "Empty refinement from Groq.", StatusCodes.Status502BadGateway));

        return Result.Success(refined.Trim());
    }
}
