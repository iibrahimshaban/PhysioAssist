namespace PhysioAssist.Api.Modules.Auth.Services;

public class ClinicDoctorResolver(ApplicationDbContext _context) : IClinicDoctorResolver
{
    public async Task<IReadOnlyList<Guid>> GetDoctorIdsForClinicAsync(
        Guid clinicId, CancellationToken cancellationToken = default)
    {
        var doctorIdStrings = await _context.Users
            .Where(u => u.ClinicId == clinicId
                        && _context.UserRoles.Any(ur => ur.UserId == u.Id
                            && (ur.RoleId == DefaultRoles.JuniorDoctorRoleId || ur.RoleId == DefaultRoles.SoloRoleId)))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        return doctorIdStrings
            .Select(id => Guid.TryParse(id, out var guid) ? guid : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();
    }
}
