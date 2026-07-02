namespace PhysioAssist.Api.Shared.QR;

/// <summary>
/// Internal QR token payload used by QRService.
/// DO NOT use directly as an API Request DTO.
/// Create a separate Request DTO with FluentValidation when exposing QR operations via API.
/// </summary>
public class QRTokenPayload
{
    public QRTokenPurpose Purpose { get; init; }
    public Guid TargetId { get; init; }
    public DateTime Expiry { get; init; }
    public string Nonce { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
}
