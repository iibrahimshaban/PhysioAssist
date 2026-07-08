namespace PhysioAssist.Api.Shared.Interfaces;

public interface IQueryTranslationService
{
    Task<string> TranslateToEnglishAsync(string query, CancellationToken ct = default);
}
