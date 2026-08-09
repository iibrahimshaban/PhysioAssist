using PhysioAssist.Api.Shared.Interfaces.Documentation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PhysioAssist.Api.Infrastructure.Documentation;

public static class DocumnetationSystemPrompts
{
    public const string SessionSummaryPrompt = """
        You are summarizing a physical therapy session note for internal clinic records.
        You will be given the note's Subjective, Objective (structured JSON), Assessment,
        and Plan sections.
 
        Write a single short paragraph (2-4 sentences) in plain clinical language summarizing
        what happened in this session and the patient's status. This summary will later be
        combined with other session summaries to build a case overview, so keep it factual
        and self-contained — don't reference "today" or "this session" in a way that only
        makes sense in isolation.
 
        Respond with ONLY the summary paragraph. No headers, no labels, no markdown.
        """;

    public static string BuildRollUpSystemPrompt(SummaryAudience audience, SummaryScope? scope, List<string>? focusAreas)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are writing a case summary for a physical therapy patient, based on a series");
        sb.AppendLine("of per-session summaries and structured clinical findings.");
        sb.AppendLine();
        sb.AppendLine("You are NOT given the patient's name, address, or any identifying information —");
        sb.AppendLine("do not invent one, and do not refer to the patient by name. Refer to them as");
        sb.AppendLine("\"the patient\".");
        sb.AppendLine();

        if (audience == SummaryAudience.Colleague)
        {
            sb.AppendLine("Audience: a colleague physical therapist picking up or reviewing this case.");
            sb.AppendLine("Use a clinical handoff structure (Situation, Background, Assessment,");
            sb.AppendLine("Recommendation) and clinical terminology. Include specific numbers/scores");
            sb.AppendLine("and trends across sessions where the data supports them (e.g. \"Berg score");
            sb.AppendLine("improved from 32 to 45 over 4 sessions\") rather than vague statements.");

            switch (scope)
            {
                case SummaryScope.Full:
                    sb.AppendLine("Scope: FULL — cover the complete case history across all sessions provided.");
                    break;
                case SummaryScope.Partial:
                    sb.AppendLine("Scope: PARTIAL — give a condensed overview, prioritizing the most recent");
                    sb.AppendLine("sessions and the overall trajectory rather than every session in detail.");
                    break;
                case SummaryScope.Focused:
                    sb.AppendLine("Scope: FOCUSED — only include content relevant to the following focus areas,");
                    sb.AppendLine($"omit everything else: {string.Join(", ", focusAreas ?? [])}.");
                    break;
            }
        }
        else
        {
            sb.AppendLine("Audience: the patient themselves, reading about their own progress.");
            sb.AppendLine("Use plain, warm, non-technical language. No clinical jargon, no raw scale");
            sb.AppendLine("scores or acronyms (translate them into what they mean practically instead,");
            sb.AppendLine("e.g. \"your balance has improved\" rather than \"Berg score 45\"). Focus on");
            sb.AppendLine("what has improved and encourage continued engagement with treatment.");
        }

        sb.AppendLine();
        sb.AppendLine("Respond with ONLY the summary text. No headers, no markdown, no session-by-session list.");

        return sb.ToString();
    }

    public static string BuildRollUpUserContent(List<SessionSummaryInput> sessions)
    {
        var payload = sessions.Select((s, i) => new
        {
            session_number = i + 1,
            narrative_summary = s.NarrativeSummary,
            objective_findings = s.ObjectiveFindingsJson
        });

        return JsonSerializer.Serialize(payload);
    }
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
