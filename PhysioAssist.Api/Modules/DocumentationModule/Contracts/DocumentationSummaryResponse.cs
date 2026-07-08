namespace PhysioAssist.Api.Modules.DocumentationModule.Contracts;

public sealed record DocumentationSummaryResponse(
    Guid Id,
    SummaryAudience Audience,
    SummaryScope? Scope,
    List<string>? FocusAreas,
    bool AnonymizePersonalData,
    string SummaryText,
    string FileUrl,
    DateTime GeneratedAt
    );
