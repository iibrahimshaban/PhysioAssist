using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Interfaces.Ingestion;

namespace PhysioAssist.Api.Infrastructure.Embeddding;

public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingOptions _options;

    public GeminiEmbeddingService(HttpClient httpClient, IOptions<EmbeddingOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        // Gemini uses a dedicated header, not Bearer auth
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _options.Token);
    }

    public async Task<SqlVector<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var url = $"{_options.Endpoint}/{_options.EmbeddingModel}:embedContent";

        var payload = new
        {
            content = new { parts = new[] { new { text } } },
            outputDimensionality = _options.Dimensions
        };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbedContentResponse>(cancellationToken: ct);

        return new SqlVector<float>(result!.Embedding.Values);
    }

    // Gemini's batch endpoint takes a list of individual embed requests, each naming the model
    public async Task<List<SqlVector<float>>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken ct = default)
    {
        var url = $"{_options.Endpoint}/{_options.EmbeddingModel}:batchEmbedContents";

        var payload = new
        {
            requests = texts.Select(text => new
            {
                model = $"models/{_options.EmbeddingModel}",
                content = new { parts = new[] { new { text } } },
                outputDimensionality = _options.Dimensions
            }).ToArray()
        };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BatchEmbedContentResponse>(cancellationToken: ct);

        return result!.Embeddings.Select(e => new SqlVector<float>(e.Values)).ToList();
    }

    private sealed record EmbedContentResponse(EmbeddingValues Embedding);
    private sealed record BatchEmbedContentResponse(EmbeddingValues[] Embeddings);
    private sealed record EmbeddingValues(float[] Values);
}
