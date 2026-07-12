using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.Agents;
using PhysioAssist.Api.Modules.QueryModule.Interfaces;

namespace PhysioAssist.Api.Modules.QueryModule.Controllers
{

    public class QueryAgentController : Controller
    {
        private readonly ChatCompletionAgent _agent;
        private readonly IChatHistoryStore _historyStore;

        public QueryAgentController(ChatCompletionAgent chatCompletionAgent, IChatHistoryStore historyStore)
        {
            _agent = chatCompletionAgent;
            _historyStore = historyStore;
        }

        /* 
         * Add Or Get a Conversation ID --> Created at the front end
         * If exist in dictionary --> Gets the history back for AI
         * If does not exist --> Create new ChatHistory with the passed Conversation ID
         */
        [HttpGet("ask")]
        public async Task<IActionResult> Ask([FromQuery] string conversationId, [FromQuery] string question, CancellationToken ct)
        {
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

            // Manually trigger reduction
            #pragma warning disable SKEXP0110
            if (_agent.HistoryReducer is not null)
            {
                var reduced = await _agent.HistoryReducer.ReduceAsync(history, ct);
                if (reduced is not null)
                {
                    history.Clear();
                    history.AddRange(reduced);
                }
            }



            return Ok(new
            {
                ConversationId = conversationId,
                Question = question,
                Answer = answer
            });
        }


        /* 
         * Clear a chat with the passed conversation ID
         * If exist in dictionary --> Returns true and removes successfully
         * If does not exist --> Returns false
         */

        [HttpGet("clear")]
        public async Task<IActionResult> Clear([FromQuery] string conversationId)
        {
            var result = _historyStore.Clear(conversationId);

            if (result.IsSuccess)
                return Ok(new { result = result.Value, response = "The conversation cleared successfully" });

            return NotFound(new { result = false , response = "The conversation does not exist" });
        }

        /*
         * USED TO TRACK HISTORY TO CHECK IF SUMMARIZATION WORKS FINE OR NOT
         */
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
}
