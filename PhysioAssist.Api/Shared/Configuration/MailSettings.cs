using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Shared.Configuration;

public class MailSettings
{
    [Required, EmailAddress]
    public string Mail { get; set; } = string.Empty;
    [Required]
    public string DisplayName { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
    [Required]
    public string Host { get; set; } = string.Empty;
    [Required]
    public int Port { get; set; }
}