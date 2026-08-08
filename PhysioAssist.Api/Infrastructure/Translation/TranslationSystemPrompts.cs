namespace PhysioAssist.Api.Infrastructure.Translation;

public static class TranslationSystemPrompts
{

    public const string TranslateToArabicPrompt =
        "Translate the following clinical text into natural, fluent Arabic for a physiotherapist. " +
        "Rules you must follow exactly:\n" +
        "1. Keep medical, anatomical, and clinical technical terms in English exactly as written " +
        "(e.g. lumbar radiculopathy, L4-L5, straight leg raise, McKenzie, dermatome, extension, " +
        "flexion, grade 1, acute stage, centralization). This also includes imaging/equipment " +
        "abbreviations (MRI, CT, X-ray, TENS, KAFO) and any named technique or program — " +
        "do NOT transliterate or translate these terms under any circumstances, even though the " +
        "rest of the sentence around them is in Arabic.\n" +
        "2. Translate all general sentence structure, connecting words, and explanations into Arabic.\n" +
        "3. Preserve ALL markdown formatting exactly as-is: **bold** markers, ### headings, - bullet " +
        "points, numbered lists, and line breaks must remain in the same positions relative to the " +
        "translated content. Do not strip or alter markdown syntax characters.\n" +
        "4. Do not add any commentary, explanation, or notes. Return ONLY the translated text.\n" +
        "5. Do not translate proper nouns like patient names.\n" +
        "\nEXAMPLE:\n" +
        "Input: \"MRI confirmed an L4-L5 disc bulge. TENS was used for pain control.\"\n" +
        "Correct output: \"أكد الـMRI وجود انتفاخ في القرص بين L4-L5. تم استخدام TENS للسيطرة على الألم.\"\n" +
        "Incorrect output (do NOT do this): \"أكد الرنين المغناطيسي وجود انتفاخ في القرص بين الفقرة " +
        "الرابعة والخامسة القطنية. تم استخدام جهاز التحفيز الكهربائي للسيطرة على الألم.\"\n";

}
