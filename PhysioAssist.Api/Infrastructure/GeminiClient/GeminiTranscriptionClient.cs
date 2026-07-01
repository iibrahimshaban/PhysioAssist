using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Dtos.Transcription;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.SystemPrompts;
using System.Text;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.GeminiClient;

public sealed class GeminiTranscriptionClient : IAudioTranscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiTranscriptionClient(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    public async Task<Result<TranscriptionResult>> TranscribeAsync(
        TranscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Buffer the stream — base64 requires full bytes in memory
        using var buffer = new MemoryStream();
        await request.AudioStream.CopyToAsync(buffer, cancellationToken);
        var audioBytes = buffer.ToArray();
        var base64Audio = Convert.ToBase64String(audioBytes);

        var mimeType = ResolveMimeType(request.FileName);

        var systemInstruction = string.IsNullOrWhiteSpace(request.Prompt)
            ? TranscriptionPrompts.GeminiMedicalTranscription
            : $"{TranscriptionPrompts.GeminiMedicalTranscription}\n\nAdditional context: {request.Prompt}";

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64Audio
                            }
                        },
                        new { text = "Transcribe this audio." }
                    }
                }
            }
        };

        var body = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var url = $"models/{_options.TranscriptionModel}:generateContent?key={_options.ApiKey}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(url, body, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<TranscriptionResult>(
                new Error("Transcription.NetworkError", ex.Message, StatusCodes.Status503ServiceUnavailable));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TranscriptionResult>(
                new Error("Transcription.Timeout", "Request to Gemini timed out.", StatusCodes.Status504GatewayTimeout));
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<TranscriptionResult>(
                new Error("Transcription.ProviderError",
                    $"Gemini returned {(int)response.StatusCode}: {errorBody}",
                    (int)response.StatusCode));
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<TranscriptionResult>(
                new Error("Transcription.EmptyResponse", "Empty response from Gemini.", StatusCodes.Status502BadGateway));

        return Result.Success(new TranscriptionResult(
            text.Trim(),
            text.Trim(),   // RefinedText same as RawText — Gemini does both in one shot
            AudioLanguage.Mixed,
            null));
    }

    private static string ResolveMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".mp3" => "audio/mp3",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".webm" => "audio/webm",
            ".flac" => "audio/flac",
            _ => "audio/mp3"
        };
}
