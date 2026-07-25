using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace PhysioAssist.Api.Modules.QueryModule.Plugin;

public class AnswerTranslationPlugin(IAnswerTranslationService translationService)
{
    private readonly IAnswerTranslationService _translationService = translationService;

    [KernelFunction, Description(
        "Translates your already-composed English clinical answer into Arabic. " +
        "Call this ONLY when the doctor explicitly asks you to translate your answer to Arabic " +
        "(e.g. they say 'ترجم', 'بالعربي', 'in Arabic', 'translate this'). " +
        "Pass your full, complete, already-formatted markdown answer as-is — do not shorten it. " +
        "Medical terms will automatically stay in English in the output; do not pre-translate them yourself.")]
    public async Task<string> TranslateAnswerToArabic(
        [Description("The complete English markdown-formatted answer to translate")] string englishAnswer)
    {
        return await _translationService.TranslateToArabicAsync(englishAnswer);
    }
}
