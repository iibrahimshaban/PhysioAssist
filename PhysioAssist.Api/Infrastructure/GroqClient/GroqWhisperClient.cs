using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Dtos.Transcription;
using PhysioAssist.Api.Shared.Interfaces.Common;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.GroqClient;

public sealed class GroqWhisperClient : IAudioTranscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;

    public GroqWhisperClient(HttpClient httpClient, IOptions<GroqOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    public async Task<Result<TranscriptionResult>> TranscribeAsync(
        TranscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        var streamContent = new StreamContent(request.AudioStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", request.FileName);
        content.Add(new StringContent(_options.TranscriptionModel), "model");
        content.Add(new StringContent("json"), "response_format");

        if (!string.IsNullOrWhiteSpace(request.LanguageHint))
            content.Add(new StringContent(request.LanguageHint), "language");

        if (!string.IsNullOrWhiteSpace(request.Prompt))
            content.Add(new StringContent(request.Prompt), "prompt");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "audio/transcriptions")
        {
            Content = content
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        // Debugging snippet

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<TranscriptionResult>(
                new Error("Transcription.NetworkError", ex.Message, StatusCodes.Status503ServiceUnavailable)
                );
               
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TranscriptionResult>(
                new Error("Transcription.Timeout", "Request to Groq timed out.", StatusCodes.Status504GatewayTimeout)
                );
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            return Result.Failure<TranscriptionResult>(
               new Error(
                "Transcription.ProviderError",
                $"Groq returned {(int)response.StatusCode}: {errorBody}",
                (int)response.StatusCode)
               );
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonSerializer.Deserialize<GroqTranscriptionResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (parsed is null)
            return Result.Failure<TranscriptionResult>(
                new Error("Transcription.EmptyResponse", "Empty response from Groq.", StatusCodes.Status502BadGateway)
                );

        return Result.Success(new TranscriptionResult(
            parsed.Text,
            null!,
            MapLanguage(parsed.Language),
            parsed.Duration)
            );
    }
    private static AudioLanguage MapLanguage(string? raw) => raw?.ToLowerInvariant() switch
    {
        "arabic" or "ar" => AudioLanguage.Arabic,
        "english" or "en" => AudioLanguage.English,
        _ => AudioLanguage.Mixed
    };

    private sealed record GroqTranscriptionResponse(string Text, string? Language, double? Duration);
}
