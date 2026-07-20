namespace PhysioAssist.Api.Shared.Interfaces.Ingestion;

public interface IQueryTranslationService
{
    Task<string> TranslateToEnglishAsync(string query, CancellationToken ct = default);
}
