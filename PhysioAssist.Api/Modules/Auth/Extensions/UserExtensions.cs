using System.Security.Claims;

namespace PhysioAssist.Api.Modules.Auth.Extensions;

public static class UserExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
