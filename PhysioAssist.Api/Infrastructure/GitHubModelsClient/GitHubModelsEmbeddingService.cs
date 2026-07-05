using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Options;
using PhysioAssist.Api.Shared.Interfaces;
using System.Net.Http.Headers;

namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public class GitHubModelsEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly GitHubModelsEmbeddingOptions _options;

    public GitHubModelsEmbeddingService(HttpClient httpClient, IOptions<GitHubModelsEmbeddingOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<SqlVector<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var payload = new
        {
            model = _options.EmbeddingModel,
            input = new[] { text }
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);

        return new SqlVector<float>(result.Data[0].Embedding);
    }

    private sealed record EmbeddingResponse(EmbeddingData[] Data);
    private sealed record EmbeddingData(float[] Embedding);
}
