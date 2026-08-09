using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Dtos.Chunking;
using PhysioAssist.Api.Shared.Interfaces.Ingestion;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.Chunking.Sbg;

public class SbgChunkingService : ITranscriptChunkingService
{
    private readonly HttpClient _httpClient;
    private readonly SbgChunkingModelOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // SBG has no response_format/json_object param — the JSON-only instruction has to
    // carry the full weight here, same reliance as the fence-stripping fallback below.
    private static readonly string SystemPrompt =
        ChunkingPrompts.BuildFullPrompt(ChunkingFewShotExamples.Formatted);

    public SbgChunkingService(HttpClient httpClient, IOptions<SbgChunkingModelOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<List<ExtractedChunk>> ExtractChunksAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var payload = new
        {
            model_id = _options.ModelId,
            messages = new object[]
            {
                new { role = "user", content = text }
            },
            system_prompt = SystemPrompt,
            max_tokens = 4096
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"{_options.BaseUrl}{_options.ChatPath}", payload, ct);
        response.EnsureSuccessStatusCode();

        var rawBody = await response.Content.ReadAsStringAsync(ct);
        var content = SbgResponseParser.ExtractContent(rawBody)?.Trim();

        if (string.IsNullOrWhiteSpace(content))
            return [];

        // Defensive fence-stripping — same insurance as the NVIDIA/GLM version, since
        // no model reliably skips markdown fences even when told not to
        content = content.Trim().Trim('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();

        try
        {
            var wrapper = JsonSerializer.Deserialize<ChunksWrapper>(content, JsonOptions);
            return wrapper?.Chunks ?? [];
        }
        catch (JsonException)
        {
            // Malformed JSON from the model — don't crash the pipeline, return empty
            // and let the caller's "NoChunks" error path handle it
            return [];
        }
    }

    private sealed record ChunksWrapper(List<ExtractedChunk> Chunks);
}
