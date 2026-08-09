using PhysioAssist.Api.Shared.Interfaces.Documentation;

namespace PhysioAssist.Api.Shared.Dtos.Documentation;

public sealed record GenerateAiSummaryResponse(
    SessionProgressNoteResponse ProgressNote,
    NarrativeDraftResult? NarrativeDraft
    );
