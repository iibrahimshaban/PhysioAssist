using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.Agents;
using PhysioAssist.Api.Modules.QueryModule.Interfaces;
using System.Text;
using System.Text.Json;

namespace PhysioAssist.Api.Modules.QueryModule.Controllers
{
#pragma warning disable SKEXP0110
    [Route("api")]
    [ApiController]
    public class QueryAgentController : ControllerBase
    {
        private readonly ChatCompletionAgent _agent;
        private readonly IChatHistoryStore _historyStore;
        private readonly ILogger<QueryAgentController> _logger;

        public QueryAgentController(ChatCompletionAgent chatCompletionAgent, IChatHistoryStore historyStore, ILogger<QueryAgentController> logger)
        {
            _agent = chatCompletionAgent;
            _historyStore = historyStore;
            _logger = logger;
        }

        [HttpGet("ask")]
        [HasPermission(Permissions.WriteQueryAgent)]
        public async Task<IActionResult> Ask([FromQuery] string conversationId, [FromQuery] string question, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return BadRequest("conversationId is required.");

            if (string.IsNullOrWhiteSpace(question))
                return BadRequest("Question is required.");

            var history = _historyStore.Get(conversationId);

            history.AddUserMessage(question);

            var responses = new List<string>();

            await foreach (var item in _agent.InvokeAsync(history, cancellationToken: ct))
            {
                var message = item.Message;
                if (!string.IsNullOrWhiteSpace(message.Content))
                    responses.Add(message.Content);
            }

            var answer = string.Join("\n", responses);

            history.AddAssistantMessage(answer);

            return Ok(new
            {
                ConversationId = conversationId,
                Question = question,
                Answer = answer
            });
        }

        [HttpGet("ask/stream")]
        [HasPermission(Permissions.WriteQueryAgent)]
        public async Task<IActionResult> AskStream([FromQuery] string conversationId, [FromQuery] string question, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return BadRequest("conversationId is required.");

            if (string.IsNullOrWhiteSpace(question))
                return BadRequest("Question is required.");

            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Append("X-Accel-Buffering", "no");

            var history = _historyStore.Get(conversationId);

            history.AddUserMessage(question);

            var answer = new StringBuilder();

            try
            {
                await foreach (var chunk in _agent.InvokeStreamingAsync(history, cancellationToken: ct))
                {
                    var delta = chunk.Message.Content;
                    if (string.IsNullOrEmpty(delta))
                        continue;

                    answer.Append(delta);
                    await WriteSseEventAsync(JsonSerializer.Serialize(new { delta }), ct);
                }

                history.AddAssistantMessage(answer.ToString());

                await WriteSseEventAsync("{}", ct, eventName: "done");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Microsoft.SemanticKernel.HttpOperationException ex)
            {
                _logger.LogError(
                    ex,
                    "Gemini (native connector) rejected request for conversation {ConversationId}. StatusCode: {StatusCode}\nHistory:\n{History}\nResponseContent:\n{Body}",
                    conversationId,
                    ex.StatusCode,
                    JsonSerializer.Serialize(history.Select(m => new { Role = m.Role.ToString(), m.Content })),
                    ex.ResponseContent);

                await WriteSseEventAsync(
                    JsonSerializer.Serialize(new { error = "The assistant is temporarily unavailable. Please try again." }),
                    ct,
                    eventName: "error");
            }

            return new EmptyResult();
        }

        [HttpGet("clear")]
        [HasPermission(Permissions.WriteQueryAgent)]
        public async Task<IActionResult> Clear([FromQuery] string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return BadRequest("conversationId is required.");
            var result = _historyStore.Clear(conversationId);

            if (result.IsSuccess)
                return Ok(new { result = result.Value, response = "The conversation cleared successfully" });

            return NotFound(new { result = false, response = "The conversation does not exist" });
        }

        private async Task WriteSseEventAsync(string json, CancellationToken ct, string? eventName = null)
        {
            if (eventName is not null)
                await Response.WriteAsync($"event: {eventName}\n", ct);

            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        [HttpGet("debug/history")]
        public IActionResult DebugHistory([FromQuery] string conversationId)
        {
            var history = _historyStore.Get(conversationId);
            return Ok(history.Select(m => new
            {
                Role = m.Role.ToString(),
                Content = m.Content,
                Length = m.Content?.Length ?? 0
            }));
        }
    }
#pragma warning restore SKEXP0110
}