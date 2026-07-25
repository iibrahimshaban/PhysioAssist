using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Shared.QR;

public class QRTokenOptions
{
    [Required]
    public const string SectionName = "QR";
    public string SigningKey { get; init; } = string.Empty;
}
