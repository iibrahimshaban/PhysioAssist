using Microsoft.Extensions.Options;

namespace PhysioAssist.Api.Infrastructure.AutoComplete
{
    public class MultiLanguageVocabularyLoader
    {
        private readonly ILogger<MultiLanguageVocabularyLoader> _logger;
        private readonly IWebHostEnvironment _env;

        // Configuration section maps language → file path + weight multiplier.
        // Externalized config = ops can add languages without code changes.
        private readonly VocabularySources _sources;

        public MultiLanguageVocabularyLoader(ILogger<MultiLanguageVocabularyLoader> logger, IWebHostEnvironment env, IOptions<VocabularySources> sources)
        {
            _logger = logger;
            _env = env;
            _sources = sources.Value;
        }

        public async Task LoadAllAsync(MultiLanguageTrieRegistry registry, CancellationToken ct)
        {
            // Load all languages in parallel — dramatically faster startup.
            // Each Trie is independent, no shared state during construction.
            var tasks = _sources.Languages.Select(async config =>
            {
                try
                {
                    var trie = await LoadLanguageAsync(config, ct);
                    registry.Register(config.Language, trie);
                }
                catch (Exception ex)
                {
                    // A single language failing shouldn't prevent the app starting.
                    // Log loudly and continue — other languages remain functional.
                    _logger.LogError(ex, "Failed to load vocabulary for {Language} from {Path}", config.Language, config.FilePath);
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task<Trie> LoadLanguageAsync(LanguageSource config, CancellationToken ct)
        {
            var trie = new Trie();
            var path = Path.IsPathRooted(config.FilePath)
                ? config.FilePath
                : Path.Combine(_env.ContentRootPath, config.FilePath);

            if (!File.Exists(path))
            {
                _logger.LogWarning("Vocab file missing: {Path}", path);
                return trie;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var count = 0;

            // Stream file line-by-line. Handles hundred MB files.
            await foreach (var line in File.ReadLinesAsync(path, ct))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) 
                    continue;

                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                var term = parts[0];

                // Parse weight; multiply by language-level multiplier from config.
                // Lets you boost medical terms over general dictionary words.
                var rawWeight = parts.Length > 1 && int.TryParse(parts[1], out var w) ? w : 1;
                var weight = (int)(rawWeight * config.WeightMultiplier);

                var category = parts.Length > 2 ? parts[2] : config.DefaultCategory;

                trie.Insert(term, config.Language, weight, category);
                count++;

                // Periodic GC hint for very large loads — helps avoid gen-2 spike.
                if (count % 100_000 == 0)
                {
                    _logger.LogDebug("Loaded {Count} terms for {Language}...", count, config.Language);
                }
            }

            sw.Stop();
            _logger.LogInformation(
                "Loaded {Count} {Language} terms in {Ms}ms",
                trie.Count, config.Language, sw.ElapsedMilliseconds);

            return trie;
        }
    }


    public class VocabularySources
    {
        public List<LanguageSource> Languages { get; set; } = new();
    }

    public class LanguageSource
    {
        public Language Language { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public double WeightMultiplier { get; set; } = 1.0;
        public string? DefaultCategory { get; set; }
    }
}
