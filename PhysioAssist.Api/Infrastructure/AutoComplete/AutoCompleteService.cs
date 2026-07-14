using PhysioAssist.Api.Infrastructure.AutoComplete.Models;
using PhysioAssist.Api.Shared.Interfaces.Common;

namespace PhysioAssist.Api.Infrastructure.AutoComplete
{
    public class AutoCompleteService : IAutoCompleteService
    {
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


        /// <summary>
        /// List of words that matches the provided prefix 
        /// </summary>
        /// <param name="prefix">Word's prefix</param>
        /// <param name="limit">Number of words to return</param>
        /// <param name="ct">Cancelation token</param>
        /// <returns>List of read-only words</returns>
        public async Task<Result<IReadOnlyList<Suggestion>>> GetSuggestionsAsync(string prefix, int limit, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
                return Result.Failure<IReadOnlyList<Suggestion>>(new Error("Invalid Input", "No Suggestions Found", 404)); 

            limit = Math.Clamp(limit, 1, 25);

            // Detect language from user input
            // First character is enough
            var language = TextNormalizer.DetectLanguage(prefix);

            if (language == Language.Unknown)
            {
                _logger.LogDebug("Unknown language for prefix (length {Length})", prefix.Length);
                return Result.Failure<IReadOnlyList<Suggestion>>(new Error("Language Not Found", $"Unknown language for prefix (length {prefix.Length})", 404));
            }

            var trie = _registry.Get(language);
            if (trie is null)
            {
                _logger.LogWarning("No trie registered for {Language}", language);
                return Result.Failure<IReadOnlyList<Suggestion>>(new Error("No Data", $"No trie registered for {language}", 404));
            }


            var vocabTask = Task.FromResult(trie.Search(prefix, language, limit));

            await Task.WhenAll(vocabTask);

            var vocabMatches = await vocabTask;


            return Result.Success<IReadOnlyList<Suggestion>>(vocabMatches.Select(m => new Suggestion(m.Term, m.Category, m.BaseWeight, language.ToString())).ToList());
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
