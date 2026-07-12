namespace PhysioAssist.Api.Shared.Dtos.Documentation;

public sealed record SessionProgressNoteResponse(
    Guid Id,
    Guid SessionId,
    Guid DocumentationTemplateId,
    string Subjective,
    string? ObjectiveFindings,
    string Assessment,
    string Plan
    );
