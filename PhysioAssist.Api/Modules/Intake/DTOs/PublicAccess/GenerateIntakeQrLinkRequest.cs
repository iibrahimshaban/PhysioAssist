namespace PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;

public record GenerateIntakeQrLinkRequest
{
    public int ExpiryMonths { get; init; }
}
