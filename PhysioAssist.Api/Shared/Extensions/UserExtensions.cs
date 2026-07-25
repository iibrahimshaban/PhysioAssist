using System.Security.Claims;

namespace PhysioAssist.Api.Shared.Extensions;

public static class UserExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
    public static async Task<Guid?> GetDoctorIdAsync(
       this ClaimsPrincipal user,
       ApplicationDbContext context,
       CancellationToken cancellationToken = default)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return null;

        if (user.IsInRole(DefaultRoles.Receptionist))
        {
            return await context.Receptionists
                .Where(r => r.UserId == userId)
                .Select(r => (Guid?)r.ManagingDoctorId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return Guid.TryParse(userId, out var doctorId) ? doctorId : null;
    }
}
