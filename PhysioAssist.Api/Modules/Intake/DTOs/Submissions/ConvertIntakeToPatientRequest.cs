namespace PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

public record ConvertIntakeToPatientRequest
{
    public string? Notes { get; init; }
}
