namespace PhysioAssist.Api.Shared.QR;

public class QRTokenPayload
{
    public QRTokenPurpose Purpose { get; init; }
    public Guid TargetId { get; init; }
    public DateTime Expiry { get; init; }
    public string Nonce { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
}
