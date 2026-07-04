//namespace PhysioAssist.Api.Infrastructure.AutoComplete
//{
//    public class VocabularyLoader
//    {
//        private readonly ILogger<VocabularyLoader> _logger;

//        // IWebHostEnvironment gives access to ContentRootPath (project root).
//        private readonly IWebHostEnvironment _env;

//        public VocabularyLoader(ILogger<VocabularyLoader> logger, IWebHostEnvironment env)
//        {
//            _logger = logger;
//            _env = env;
//        }

//        // async Task<Trie> because file I/O should be async.
//        // CancellationToken lets the caller abort long operations.
//        public async Task<Trie> LoadAsync(CancellationToken ct = default)
//        {
//            var trie = new Trie();
//            var path = Path.Combine(_env.ContentRootPath, "Data", "pt-vocabulary.txt");


//            // log a warning but return empty trie.
//            // App still starts; you'll see zero suggestions until file is fixed.
//            if (!File.Exists(path))
//            {
//                _logger.LogWarning("Vocabulary file not found at {Path}", path);
//                return trie;
//            }

//            var sw = System.Diagnostics.Stopwatch.StartNew();
//            var lineCount = 0;


//            await foreach (var line in File.ReadLinesAsync(path, ct))
//            {
//                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

//                // Split on '|' and trim each part. Format: term|weight|category
//                var parts = line.Split('|', StringSplitOptions.TrimEntries);
//                if (parts.Length == 0) continue;

//                var term = parts[0];
//                var weight = parts.Length > 1 && int.TryParse(parts[1], out var w) ? w : 1;
//                var category = parts.Length > 2 ? parts[2] : null;

//                //trie.Insert(term, weight, category);
//                lineCount++;
//            }

//            sw.Stop();
//            _logger.LogInformation(
//                "Loaded {Count} terms into trie in {Ms}ms",
//                trie.Count, sw.ElapsedMilliseconds);

//            return trie;
//        }
//    }
//}
