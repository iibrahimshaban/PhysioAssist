using Microsoft.AspNetCore.Identity;

namespace PhysioAssist.Api.Modules.Auth.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public bool IsDisabled { get; set; } = false;
    public Doctor? Doctor { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();
    public ICollection<OtpEntry> OtpEntries { get; set; } = new HashSet<OtpEntry>();
}
