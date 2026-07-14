namespace PhysioAssist.Api.Shared.Dtos.Intake;

public record PatientIntakeSummaryResponse(
    string? PatientFullName,
    string? Gender,
    int? Age,
    string? ChiefComplaint,
    string? InjuryDescription,
    DateTime? InjuryDate,
    PatientCategory? PatientCategory
    );
