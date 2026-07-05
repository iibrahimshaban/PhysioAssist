using Org.BouncyCastle.Math;

namespace PhysioAssist.Api.Infrastructure.AutoComplete
{
    public class VocabularyBootstrapService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<VocabularyBootstrapService> _logger;

        public VocabularyBootstrapService(IServiceProvider services, ILogger<VocabularyBootstrapService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bootstrapping multi-language vocabularies...");

            // Resolve services from root container (safe — these are singletons).
            var registry = _services.GetRequiredService<MultiLanguageTrieRegistry>();
            var loader = _services.GetRequiredService<MultiLanguageVocabularyLoader>();

            // Do the actual async load.
            await loader.LoadAllAsync(registry, cancellationToken);

            var stats = registry.GetStatistics();
            _logger.LogInformation(
                "Vocabulary bootstrap complete: {Stats}",
                string.Join(", ", stats.Select(kvp => $"{kvp.Key}={kvp.Value:N0}")));
        }

        // Nothing to clean up — tries are managed by GC when registry is disposed.
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
