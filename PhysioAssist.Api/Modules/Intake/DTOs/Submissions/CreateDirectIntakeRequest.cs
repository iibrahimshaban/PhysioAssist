namespace PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

public record CreateDirectIntakeRequest(
    Guid FormSchemaId,
    string FormSubmissionData,   // raw JSON — untouched, whatever the dynamic form produced
    string? PainPointsData         // raw JSON — untouched
);