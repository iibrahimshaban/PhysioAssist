namespace PhysioAssist.Api.Modules.DocumentationModule.Contracts;

public sealed record PatientDocumentationSessionResponse(
    Guid SessionId,
    DateTimeOffset Date,
    double DurationMinutes,
    SlotStatus AttendanceStatus,
    bool HasProgressNote,
    bool HasSummary,
    bool IsSummaryStale,
    string? NarrativeSummary
    );
