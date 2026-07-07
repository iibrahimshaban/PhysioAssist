namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public static class SystemPrompts
{
    private const string IntroAndRules =
        "You are extracting structured clinical records from a physiotherapy session transcript, " +
        "for storage as an ENGLISH-ONLY searchable knowledge base doctors will query later. " +
        "The original transcript may mix Arabic and English (medical terms often already in English). " +
        "\n\nCRITICAL RULES:\n" +
        "1. Split into a SEPARATE object for each DISTINCT body region or treatment domain discussed.\n" +
        "2. Translate ALL fields into clear, natural clinical English. Do not leave any Arabic words " +
        "or phrases in the output, even if mixed with English in the source. Do not do a literal " +
        "word-for-word translation — write as a clinician would naturally phrase it in English, " +
        "while preserving every clinical fact and specific detail exactly.\n" +
        "3. Do NOT invent details that weren't said. Do NOT summarize away specifics — keep exercise " +
        "names, body regions, and details as specific as the doctor said them, just in English.\n" +
        "4. Diagnosis is ONLY the condition/stage — not a catch-all. Use Notes for anything else.\n" +
        "5. If the doctor gives a closing summary or percentage breakdown that just restates topics " +
        "already covered in other objects (e.g. \"90% of the session was X and Y combined\"), do NOT " +
        "create a separate object for it — that information is redundant with objects already extracted. " +
        "Only create an object for a genuinely distinct exercise/treatment domain not covered elsewhere.\n" +
        "6. Pay attention to things the doctor explicitly avoided or excluded doing, and why " +
        "(e.g. \"we didn't do squats because of a past injury/mistake\") — this belongs in Notes, " +
        "not omitted.\n" +
        "7. Capture clinical reasoning and explanations the doctor gives — compensatory mechanisms, " +
        "why a technique works, anatomical explanations for a patient's movement strategy — even " +
        "when it's not a direct instruction/action. This belongs in Notes if it doesn't fit " +
        "RecommendationDetails.\n" +
        "8. Do not force English medical/technical terms into different words than what's standard — " +
        "e.g. keep \"KAFO\", \"quadratus lumborum\", \"dorsiflexion\" as-is; only translate the " +
        "connective Arabic phrasing around them, not established clinical terminology.\n" +
        "9. Do not add clinical specificity the doctor didn't state — e.g. if the doctor says " +
        "\"disc grade 1\" without naming the pathology type (herniation, bulge, etc.), keep it as " +
        "stated. Do not infer or complete a diagnosis with plausible-sounding detail that wasn't said.\n" +
        "10. If the doctor corrects or clarifies themselves mid-sentence (e.g. says one term, then " +
        "restates it as a different, more accurate term, with a reason), preserve that correction " +
        "explicitly — state the corrected/accurate term in Diagnosis, and note the correction and " +
        "reason in Notes. Do not silently smooth over self-corrections during translation, and do " +
        "not let an earlier looser word choice bleed into Diagnosis after a correction was made.\n";

    private const string JsonShapeInstruction =
        "\nReturn ONLY a JSON array matching this exact shape, no markdown fences, no extra text:\n[{{\n{0}}}]\n\n";

    public static string BuildFullPrompt(string fewShotExample) =>
        IntroAndRules +
        string.Format(JsonShapeInstruction, ExtractedChunkPromptSchemaBuilder.BuildFieldDescriptions()) +
        fewShotExample;

    public const string TranslateToEnglishPrompt =
        "Translate the user's search query into clear clinical English. " +
        "If it's already in English, return it unchanged. " +
        "Preserve medical/technical terms exactly (e.g. KAFO, dorsiflexion, spasticity). " +
        "Return ONLY the translated query text, nothing else — no quotes, no explanation.";

}