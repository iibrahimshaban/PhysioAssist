using Microsoft.AspNetCore.Identity;

namespace PhysioAssist.Api.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public bool IsDisabled { get; set; } = false;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
