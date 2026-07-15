namespace PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;

public record GenerateIntakeQrLinkRequest
{
    public int ExpiryHours { get; init; }
}
