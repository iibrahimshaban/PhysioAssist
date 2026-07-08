using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Shared.QRService;

public class QrSettings
{
    public const string SectionName = "QrSettings";

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    public int DefaultExpiryDays { get; set; } = 365;
}
