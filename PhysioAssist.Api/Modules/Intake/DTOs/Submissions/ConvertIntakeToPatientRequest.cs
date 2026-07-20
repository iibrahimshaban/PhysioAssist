namespace PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

public record ConvertIntakeToPatientRequest
{
    public string? FormSubmissionData { get; init; } // ADDED — doctor's edited answers, if any
    public string? PainPointsData { get; init; }      // ADDED — doctor's edited pain map, if any
}
