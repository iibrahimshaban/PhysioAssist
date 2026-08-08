using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhysioAssist.Api.Shared.Common;

internal sealed record SbgChatResponse(
    [property: JsonPropertyName("request_id")] string RequestId,
    [property: JsonPropertyName("model_id")] string ModelId,
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("output_text")] string? OutputText,
    [property: JsonPropertyName("usage")] SbgUsage? Usage,
    [property: JsonPropertyName("status")] string? Status
);

internal sealed record SbgUsage(
    [property: JsonPropertyName("input_tokens")] int InputTokens,
    [property: JsonPropertyName("output_tokens")] int OutputTokens,
    [property: JsonPropertyName("total_tokens")] int TotalTokens,
    [property: JsonPropertyName("stop_reason")] string? StopReason,
    [property: JsonPropertyName("budget_state")] string? BudgetState,
    [property: JsonPropertyName("fallback_used")] bool FallbackUsed
);

internal static class SbgResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string? ExtractContent(string rawJson)
    {
        SbgChatResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<SbgChatResponse>(rawJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (parsed is null || !string.Equals(parsed.Status, "active", StringComparison.OrdinalIgnoreCase))
            return null;

        return string.IsNullOrWhiteSpace(parsed.OutputText) ? null : parsed.OutputText;
    }
}
