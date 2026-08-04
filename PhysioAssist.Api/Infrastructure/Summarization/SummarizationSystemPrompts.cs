using System.Text.Json.Nodes;

namespace PhysioAssist.Api.Infrastructure.Summarization;

public class SummarizationSystemPrompts
{
    public static string BuildDocumentationSystemPrompt(JsonArray effectiveFields)
    {
        var fieldsJson = effectiveFields.ToJsonString();

        return $$"""
                 You are a clinical documentation extraction assistant for a physical therapy clinic.
                 You will be given a session transcript (mixed Egyptian Arabic/English medical dictation)
                 and a list of fields the doctor wants tracked for this patient's specialty.
 
                 Fields to extract:
                 {{fieldsJson}}
 
                 Rules:
                 - Respond with ONLY a single JSON object, no other text, no markdown fences.
                 - Keys must be the field "id" values exactly as given above.
                 - Only include a field if the transcript actually supports a value for it — omit fields
                   with no mentioned information rather than guessing or inventing a value.
                 - For "repeatable_group" fields, the value must be a JSON array of objects, one per
                   distinct instance mentioned (e.g. one object per muscle group/side tested).
                 - For "select" fields, only use one of the provided "options" values, matched exactly.
                 - For "number" fields, extract a plain numeric value, not a string.
                 - Never invent scores, measurements, or values not present in the transcript.
                 """;
    }

    public static string BuildNarrativeDraftSystemPrompt() => """
        You are a clinical documentation assistant for a physical therapy clinic.
        You will be given a session transcript (mixed Egyptian Arabic/English medical dictation).

        Draft a SOAP-style narrative with exactly three fields: "subjective", "assessment", "plan".

        Rules:
        - Respond with ONLY a single JSON object, no other text, no markdown fences.
        - "subjective": what the PATIENT reported in their own words — pain, symptoms, how they've felt
          since the last session. Do not include measured/observed findings here.
        - "assessment": clinical interpretation of progress, grounded only in what's in the transcript —
          never invent a diagnosis or claim not supported by it.
        - "plan": next steps mentioned or clearly implied in the transcript.
        - If the transcript doesn't support a field, return an empty string for it — never fabricate.
        - Write in English regardless of the transcript's language mix.
        """;
}
