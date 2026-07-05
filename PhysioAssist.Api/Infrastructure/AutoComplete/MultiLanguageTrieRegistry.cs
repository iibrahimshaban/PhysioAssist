using System.Collections.Concurrent;

namespace PhysioAssist.Api.Infrastructure.AutoComplete
{
    /// <summary>
    /// Holds one Trie per supported language. Singleton lifetime.
    /// Thread-safe reads; writes only happen at startup or hot-reload.
    /// </summary>
    public class MultiLanguageTrieRegistry
    {
        // ConcurrentDictionary chosen for safe concurrent reads during hot-reload.
        // Under normal read load a plain Dictionary would also be safe (readers only),
        // but hot-reload writes make ConcurrentDictionary the correct choice.
        private readonly ConcurrentDictionary<Language, Trie> _tries = new();
        private readonly ILogger<MultiLanguageTrieRegistry> _logger;


        public MultiLanguageTrieRegistry(ILogger<MultiLanguageTrieRegistry> logger)
        {
            _logger = logger;
        }


        /// <summary>
        /// Register or replace the Trie for a specific language.
        /// </summary>
        public void Register(Language language, Trie trie)
        {
            // AddOrUpdate replaces the old trie with the new one.
            // Old trie becomes garbage — GC will reclaim when no more readers hold it.
            _tries.AddOrUpdate(language, trie, (_, _) => trie);

            _logger.LogInformation("Registered trie for {Language} with {Count} terms", language, trie.Count);
        }

        /// <summary>
        /// Get the trie for a language, or null if unsupported.
        /// </summary>
        public Trie? Get(Language language)
        {
            return _tries.TryGetValue(language, out var trie) ? trie : null;
        }

        /// <summary>
        /// Enumerate all loaded languages. Useful for health checks and admin UI.
        /// </summary>
        public IReadOnlyDictionary<Language, int> GetStatistics()
        {
            // Snapshot to avoid enumerating live dictionary while it changes.
            return _tries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }
    }
}
