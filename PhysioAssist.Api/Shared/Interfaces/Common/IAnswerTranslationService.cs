namespace PhysioAssist.Api.Shared.Interfaces.Common;

public interface IAnswerTranslationService
{
    Task<string> TranslateToArabicAsync(string markdownAnswer, CancellationToken ct = default);
}
