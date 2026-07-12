namespace PhysioAssist.Api.Shared.QR;

public class QRTokenOptions
{
    public const string SectionName = "QR";

    public string SigningKey { get; init; } = string.Empty;
}
