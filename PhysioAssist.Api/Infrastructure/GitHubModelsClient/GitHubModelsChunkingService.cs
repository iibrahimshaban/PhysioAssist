using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Dtos.Chunking;
using PhysioAssist.Api.Shared.Interfaces.Ingestion;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public class GitHubModelsChunkingService : ITranscriptChunkingService
{
    private readonly HttpClient _httpClient;
    private readonly GitHubModelsChatOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly string SystemPrompt =
        SystemPrompts.BuildFullPrompt(ChunkingFewShotExamples.Formatted);

    public GitHubModelsChunkingService(HttpClient httpClient, IOptions<GitHubModelsChatOptions> options)
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
            model = _options.ChatModel,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = text }
            },
            temperature = 0.0,
            response_format = new { type = "json_object" }
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct);
        var content = result?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
            return [];

        // GPT-4o sometimes wraps JSON in ```json fences despite instructions — strip defensively
        content = content.Trim().Trim('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();

        try
        {
            var wrapper = JsonSerializer.Deserialize<ChunksWrapper>(content, JsonOptions);
            return wrapper?.Chunks ?? [];
        }

        // Added response_format json_object to enforce structured JSON output from the model
        // Changed deserialization to handle wrapped {"chunks": [...]} format
        catch (JsonException)
        {
            // Malformed JSON from the model — don't crash the pipeline, return empty
            // and let the caller's "NoChunks" error path handle it
            return [];
        }
    }

    private sealed record ChatCompletionResponse(List<Choice> Choices);
    private sealed record Choice(ChatMessage Message);
    private sealed record ChatMessage(string Content);
    private sealed record ChunksWrapper(List<ExtractedChunk> Chunks);
}

