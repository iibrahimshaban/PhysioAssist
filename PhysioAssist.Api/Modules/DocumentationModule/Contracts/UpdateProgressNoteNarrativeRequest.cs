namespace PhysioAssist.Api.Modules.DocumentationModule.Contracts;

public record UpdateProgressNoteNarrativeRequest(
    string Subjective,
    string Assessment,
    string Plan
    );
