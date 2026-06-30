using PhysioAssist.Api.Modules.Auth.Entities;

namespace PhysioAssist.Api.Modules.Auth.JwtService;

public interface IJwtProvider
{
    (string Token, int ExpiresIn) GenerateToken(ApplicationUser user, IEnumerable<string> Roles, IEnumerable<string> Permissions);
    Result<string> ValidateToken(string token, bool validateLifetime = true);
}
