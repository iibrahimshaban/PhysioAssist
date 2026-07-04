using PhysioAssist.Api.Infrastructure.AutoComplete;
using PhysioAssist.Api.Infrastructure.AutoComplete.Models;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Shared.AutoComplete
{
    public class AutoCompleteService : IAutoCompleteService
    {
        //private readonly Trie _trie;
        //private readonly ILogger<AutoCompleteService> _logger;
        private readonly MultiLanguageTrieRegistry _registry;
        private readonly ILogger<AutoCompleteService> _logger;

        // Singleton — trie loaded once at startup
        //public AutoCompleteService(Trie trie, ILogger<AutoCompleteService> logger)
        //{
        //    _trie = trie;
        //    _logger = logger;
        //}

        public AutoCompleteService(MultiLanguageTrieRegistry registry, ILogger<AutoCompleteService> logger)
        {
            _registry = registry;
            _logger = logger;
        }


        public async Task<IReadOnlyList<Suggestion>> GetSuggestionsAsync(string prefix, int limit, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
                return Array.Empty<Suggestion>();

            limit = Math.Clamp(limit, 1, 25);

            // Detect language from user input
            // First character is enough
            var language = TextNormalizer.DetectLanguage(prefix);

            if (language == Language.Unknown)
            {
                _logger.LogDebug("Unknown language for prefix (length {Length})", prefix.Length);
                return Array.Empty<Suggestion>();
            }

            var trie = _registry.Get(language);
            if (trie is null)
            {
                _logger.LogWarning("No trie registered for {Language}", language);
                return Array.Empty<Suggestion>();
            }


            var vocabTask = Task.FromResult(trie.Search(prefix, language, limit));

            await Task.WhenAll(vocabTask);

            var vocabMatches = await vocabTask;


            return vocabMatches.Select(m => new Suggestion(m.Term, m.Category, m.BaseWeight, language.ToString())).ToList();
        }


        //public IReadOnlyList<Suggestion> GetSuggestions(string prefix, int limit = 8)
        //{
        //    if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
        //        return Array.Empty<Suggestion>();

        //    // Cap limit to prevent abuse
        //    limit = Math.Clamp(limit, 1, 25);

        //    var matches = _trie.Search(prefix, limit);

        //    return matches.Select(m => new Suggestion(m.Term, m.Category, m.BaseWeight))
        //                  .ToList();
        //}
    }
}
