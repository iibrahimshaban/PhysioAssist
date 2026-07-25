namespace PhysioAssist.Api.Modules.PatientModule.DTOs;

public sealed record UpdateSubmissionDataRequest(
    string FormSubmissionData,
    string? PainPointsData   // optional — null means "don't touch pain points"
);