namespace PhysioAssist.Api.Infrastructure.GroqClient.Prompts;

public static class SessionSummaryPrompts
{
    public const string SystemPrompt = """
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
}
