namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public static class SystemPrompts
{
    private const string IntroAndRules =
        "You are extracting structured clinical records from a physiotherapy session transcript, " +
        "for storage as a searchable knowledge base doctors will query later. " +
        "The transcript may mix Arabic and English (medical terms often stay in English). " +
        "\n\nCRITICAL RULES:\n" +
        "1. Split into a SEPARATE object for each DISTINCT body region or treatment domain discussed.\n" +
        "2. Do NOT translate — preserve the exact original language mix in every field.\n" +
        "3. Do NOT invent details that weren't said.\n" +
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
        "RecommendationDetails.\n";

    private const string JsonShapeInstruction =
        "\nReturn ONLY a JSON array matching this exact shape, no markdown fences, no extra text:\n[{{\n{0}}}]\n\n";

    public static string BuildFullPrompt(string fewShotExample) =>
        IntroAndRules +
        string.Format(JsonShapeInstruction, ExtractedChunkPromptSchemaBuilder.BuildFieldDescriptions()) +
        fewShotExample;
}