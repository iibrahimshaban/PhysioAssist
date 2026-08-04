using System.Text.Json.Nodes;

namespace PhysioAssist.Api.Shared.Interfaces.Documentation;

public interface IDocumentationExtractionService
{
    Task<string?> ExtractObjectiveFindingsAsync(string transcriptText, JsonArray effectiveFields, CancellationToken ct = default);
    Task<NarrativeDraftResult?> DraftNarrativeAsync(string transcriptText, CancellationToken ct = default);
}

public sealed record NarrativeDraftResult(string Subjective, string Assessment, string Plan);
