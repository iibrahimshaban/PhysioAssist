using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Shared.Email.Brevo;

public class BrevoOptions
{
    public const string SectionName = "Brevo";
    public string ApiKey { get; set; } = string.Empty;
    [Required]
    public string FromEmail { get; set; } = string.Empty;
    [Required]
    public string FromName { get; set; } = string.Empty;
}
