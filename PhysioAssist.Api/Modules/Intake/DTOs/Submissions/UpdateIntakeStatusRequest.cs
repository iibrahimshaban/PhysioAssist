namespace PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

public record UpdateIntakeStatusRequest
{
    public IntakeStatus NewStatus { get; init; }
}
