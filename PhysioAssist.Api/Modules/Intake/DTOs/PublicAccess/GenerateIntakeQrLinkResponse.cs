namespace PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;

public record GenerateIntakeQrLinkResponse
{
    public string Token { get; init; } = string.Empty;
    public string PublicUrl { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
