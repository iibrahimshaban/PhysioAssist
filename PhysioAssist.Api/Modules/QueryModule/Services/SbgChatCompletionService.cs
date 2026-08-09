using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PhysioAssist.Api.Modules.QueryModule.Options;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhysioAssist.Api.Modules.QueryModule.Services;

public class SbgChatCompletionService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly SbgQueryAgentChatOptions _options;
    private readonly ILogger<SbgChatCompletionService> _logger;
    private const int MaxToolIterations = 4;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public SbgChatCompletionService(
        HttpClient httpClient,
        IOptions<SbgQueryAgentChatOptions> options,
        ILogger<SbgChatCompletionService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var toolManifest = kernel is null ? "" : BuildToolManifest(kernel);

        var systemPrompt = string.Join("\n",
            chatHistory.Where(m => m.Role == AuthorRole.System).Select(m => m.Content ?? ""));

        if (!string.IsNullOrEmpty(toolManifest))
        {
            const string toolCallFormatExample =
                "{\"tool_call\":\"PluginName.FunctionName\",\"arguments\":{\"param\":\"value\"}}";

            systemPrompt +=
                "\n\nYou have access to these tools:\n" + toolManifest +
                "\n\nWhen you need a tool, respond with ONLY this JSON and nothing else — no other text " +
                "before or after it, and do not continue writing an answer in the same response. " +
                "You have NOT seen the tool's result yet, so do not assume or fabricate what it will " +
                "return. Stop immediately after the closing brace:\n" + toolCallFormatExample +
                "\n\nDo NOT write any sentence like 'We need to call X' or 'Let me search for that' " +
                "before the JSON. Do NOT wrap the JSON in markdown code fences. Your entire response " +
                "must be exactly the JSON object and nothing else.\n" +
                "WRONG example: We need to call WebSearch. {\"tool_call\":\"WebSearch.SearchWeb\",\"arguments\":{\"query\":\"...\"}}\n" +
                "CORRECT example: {\"tool_call\":\"WebSearch.SearchWeb\",\"arguments\":{\"query\":\"...\"}}\n" +
                "\n\nWhen you have a final answer (after receiving a real tool result, or if no tool " +
                "is needed), respond with plain text only, no JSON.";
        }

        var workingMessages = chatHistory
            .Where(m => m.Role != AuthorRole.System)
            .Select(m => new { role = m.Role == AuthorRole.Assistant ? "assistant" : "user", content = m.Content ?? "" })
            .ToList();

        for (int i = 0; i < MaxToolIterations; i++)
        {
            var payload = new
            {
                model_id = _options.ModelId,
                messages = workingMessages,
                system_prompt = systemPrompt,
                max_tokens = 2048
            };

            using var response = await _httpClient.PostAsJsonAsync(
                $"{_options.BaseUrl}{_options.ChatPath}", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(rawBody);
            var text = doc.RootElement.GetProperty("output_text").GetString()?.Trim() ?? "";

            var toolCall = TryParseToolCall(text, out var parseFailureReason);
            if (toolCall is null || kernel is null)
            {
                if (!string.IsNullOrEmpty(parseFailureReason))
                    _logger.LogWarning(
                        "SBG output started with '{{' but tool-call JSON failed to parse ({Reason}). " +
                        "Falling back to treating raw text as final answer: {Text}",
                        parseFailureReason, text);

                // Final answer — no tool call detected
                return new List<ChatMessageContent> { new(AuthorRole.Assistant, text) };
            }

            // Dispatch to the actual plugin function
            string resultText;
            try
            {
                var (pluginName, functionName) = SplitToolName(toolCall.ToolCall);
                var function = kernel.Plugins.GetFunction(pluginName, functionName);
                var args = new KernelArguments();
                foreach (var kv in toolCall.Arguments ?? new())
                    args[kv.Key] = kv.Value.ToString();

                var result = await kernel.InvokeAsync(function, args, cancellationToken);
                resultText = result.ToString();
                _logger.LogInformation("Tool {ToolCall} succeeded, result length {Length}",
                    toolCall.ToolCall, resultText.Length);
            }
            catch (Exception ex)
            {
                resultText = $"Tool error: {ex.Message}";
                _logger.LogError(ex, "Tool {ToolCall} failed to dispatch/execute", toolCall.ToolCall);
            }

            // Feed the result back as the next turn (no native "tool" role here, so approximate)
            workingMessages.Add(new { role = "assistant", content = text });
            workingMessages.Add(new { role = "user", content = $"[Tool result for {toolCall.ToolCall}]: {resultText}" });
        }

        return new List<ChatMessageContent>
        {
            new(AuthorRole.Assistant, "I wasn't able to complete that after several tool attempts.")
        };
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
        yield return new StreamingChatMessageContent(AuthorRole.Assistant, result[0].Content);
    }

    private static string BuildToolManifest(Kernel kernel)
    {
        var lines = new List<string>();
        foreach (var plugin in kernel.Plugins)
        {
            foreach (var fn in plugin)
            {
                lines.Add($"- {plugin.Name}.{fn.Name}: {fn.Description}");

                foreach (var p in fn.Metadata.Parameters)
                {
                    var required = p.IsRequired ? "required" : "optional";
                    var typeName = p.ParameterType?.Name ?? "string";
                    lines.Add($"    - param \"{p.Name}\" ({typeName}, {required}): {p.Description}");
                }
            }
        }
        return string.Join("\n", lines);
    }

    private static (string plugin, string function) SplitToolName(string toolName)
    {
        var parts = toolName.Split('.', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : ("", parts[0]);
    }

    private static ToolCallDto? TryParseToolCall(string text, out string? failureReason)
    {
        failureReason = null;

        int start = text.IndexOf('{');
        if (start == -1)
        {
            return null; // genuinely no JSON anywhere — normal final answer
        }

        int depth = 0, endIndex = -1;
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0) { endIndex = i; break; }
            }
        }
        if (endIndex == -1)
        {
            failureReason = "no balanced closing brace found";
            return null;
        }

        var jsonOnly = text[start..(endIndex + 1)];
        try
        {
            var dto = JsonSerializer.Deserialize<ToolCallDto>(jsonOnly, JsonOptions);
            if (dto is null || string.IsNullOrEmpty(dto.ToolCall))
            {
                failureReason = $"deserialized but missing tool_call field. Extracted JSON was: {jsonOnly}";
                return null;
            }
            return dto;
        }
        catch (JsonException ex)
        {
            failureReason = $"{ex.Message}. Extracted JSON was: {jsonOnly}";
            return null;
        }
    }

    private sealed class ToolCallDto
    {
        [JsonPropertyName("tool_call")] public string ToolCall { get; set; } = "";
        [JsonPropertyName("arguments")] public Dictionary<string, JsonElement>? Arguments { get; set; }
    }
}