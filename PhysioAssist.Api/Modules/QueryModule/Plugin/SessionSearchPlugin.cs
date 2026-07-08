using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;

namespace PhysioAssist.Api.Modules.QueryModule.Plugin;

public class SessionSearchPlugin(ISessionChunkSearchService searchService)
{
    private readonly ISessionChunkSearchService _searchService = searchService;

    [KernelFunction, Description("Searches physiotherapy session records using semantic search. Always pass the query in clear clinical English, regardless of what language the doctor used. If searching within one patient's history, provide their patientId (from FindPatientsByName). Omit patientId to search across all patients.")]
    public async Task<string> SearchSessionChunks(
    [Description("The clinical question or topic to search for, in English")] string englishQuery,
    [Description("Optional patient ID to restrict the search to one patient's sessions")] Guid? patientId = null,
    [Description("Number of results to return")] int topN = 5)
    {
        var result = await _searchService.SearchAsync(englishQuery, patientId, topN);

        if (!result.IsSuccess || result.Value.Count == 0)
            return "No matching session records found.";

        var grouped = result.Value
        .GroupBy(r => r.SessionId)
        .Select((g, index) => new
        {
            Label = $"Session {index + 1}",
            Records = g.ToList()
        });

        var sb = new StringBuilder();
        foreach (var session in grouped)
        {
            sb.AppendLine($"--- {session.Label} ---");
            foreach (var r in session.Records)
            {
                sb.AppendLine($"Diagnosis: {r.Diagnosis}");
                sb.AppendLine($"Recommendation: {r.Recommendations}");
                sb.AppendLine($"Details: {r.RecommendationDetails}");
                if (r.PatientResponse is not null) sb.AppendLine($"Patient response: {r.PatientResponse}");
                if (r.Notes is not null) sb.AppendLine($"Notes: {r.Notes}");
                sb.AppendLine($"Next session focus: {r.NextSessionFocus}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
