using PhysioAssist.Api.Shared.Interfaces.Documentation;
using System.Text;
using System.Text.Json;

namespace PhysioAssist.Api.Infrastructure.GroqClient;

public static class RollupSummaryPrompts
{
    public static string BuildSystemPrompt(SummaryAudience audience, SummaryScope? scope, List<string>? focusAreas)
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

    public static string BuildUserContent(List<SessionSummaryInput> sessions)
    {
        var payload = sessions.Select((s, i) => new
        {
            session_number = i + 1,
            narrative_summary = s.NarrativeSummary,
            objective_findings = s.ObjectiveFindingsJson
        });

        return JsonSerializer.Serialize(payload);
    }
}
