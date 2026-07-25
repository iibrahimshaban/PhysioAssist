namespace PhysioAssist.Api.Shared.Dtos.Intake;

public record PainRegionDto(string Id, string LabelEn, string LabelAr, int Severity);

public record PainPointsResult(
    List<PainRegionDto> Regions,
    string? ChiefComplaint,
    string? PatientCategory
);

public record PatientOverviewIntakeResult(
    string FormSubmissionData,   // raw JSON string — patient answers
    string? PainPointsJson,       // raw JSON string — just { "regions": [...] }
    string? DoctorInfoJson         // raw JSON string — just { "chiefComplaint": "...", "patientCategory": "..." }
);