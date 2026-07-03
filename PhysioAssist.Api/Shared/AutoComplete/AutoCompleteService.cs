using PhysioAssist.Api.Infrastructure.AutoComplete;
using PhysioAssist.Api.Infrastructure.AutoComplete.Models;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Shared.AutoComplete
{
    public class AutoCompleteService : IAutoCompleteService
    {
        private readonly Trie _trie;
        private readonly ILogger<AutoCompleteService> _logger;

        // Singleton — trie loaded once at startup
        public AutoCompleteService(Trie trie, ILogger<AutoCompleteService> logger)
        {
            _trie = trie;
            _logger = logger;
        }

        public IReadOnlyList<Suggestion> GetSuggestions(string prefix, int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
                return Array.Empty<Suggestion>();

            // Cap limit to prevent abuse
            limit = Math.Clamp(limit, 1, 25);

            var matches = _trie.Search(prefix, limit);

            return matches.Select(m => new Suggestion(m.Term, m.Category, m.BaseWeight))
                          .ToList();
        }
    }
}
