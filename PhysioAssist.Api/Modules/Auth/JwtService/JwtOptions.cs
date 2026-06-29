using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Modules.Auth.JwtService;

public class JwtOptions
{
    [Required]
    public static string SectionName { get; } = "Jwt";
    [Required]
    public string Key { get; set; } = string.Empty;
    [Required]
    public string Issuer { get; set; } = string.Empty;
    [Required]
    public string Audience { get; set; } = string.Empty;
    [Required]
    [Range(1, int.MaxValue)]
    public int ExpiryMinutes { get; set; }
}
