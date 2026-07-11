using Microsoft.SemanticKernel.ChatCompletion;
using PhysioAssist.Api.Modules.QueryModule.Interfaces;
using System.Collections.Concurrent;

namespace PhysioAssist.Api.Modules.QueryModule.Services
{
    public class SessionChatHistoryStore : IChatHistoryStore
    {
        private readonly ConcurrentDictionary<string, ChatHistory> _histories = new();

        public Result<bool> Clear(string conversationId)
        {
            var removed = _histories.TryRemove(conversationId, out _);
            return Result.Success(removed);
        }

        public ChatHistory Get(string conversationId)
        {
            return _histories.GetOrAdd(conversationId, _ => new ChatHistory());
        }
    }
}
