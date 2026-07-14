using System.Text.Json.Nodes;

namespace PhysioAssist.Api.Shared.Interfaces.Documentation;

public interface IDocumentationExtractionService
{
    /// <summary>
    /// Extracts structured Objective findings from a session transcript, shaped by the
    /// doctor's effective (non-hidden) template fields. Returns raw JSON text ready to
    /// store as-is in SessionProgressNote.ObjectiveFindings, or null if extraction failed.
    /// </summary>
    Task<string?> ExtractObjectiveFindingsAsync(string transcriptText, JsonArray effectiveFields, CancellationToken ct = default);
}
