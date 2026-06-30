using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.Auth.Errors;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace PhysioAssist.Api.Modules.Auth.JwtService;

public class JwtProvider(IOptions<JwtOptions> jwtOptions) : IJwtProvider
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public (string Token, int ExpiresIn) GenerateToken(ApplicationUser user
        , IEnumerable<string> Roles, IEnumerable<string> Permissions)
    {
        Claim[] claims = [
            new(JwtRegisteredClaimNames.Sub,user.Id),
                new(JwtRegisteredClaimNames.Email,user.Email!),
                new(JwtRegisteredClaimNames.GivenName,user.FirstName),
                new(JwtRegisteredClaimNames.FamilyName,user.LastName),
                new(JwtRegisteredClaimNames.Jti,Guid.CreateVersion7().ToString()),
                new(nameof(Roles),JsonSerializer.Serialize(Roles),JsonClaimValueTypes.JsonArray),
                new(nameof(Permissions),JsonSerializer.Serialize(Permissions),JsonClaimValueTypes.JsonArray)
            ];

        var SymmetricSequrityKey = new
            SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));

        var SigningCredintials = new SigningCredentials(SymmetricSequrityKey, SecurityAlgorithms.HmacSha256);

        var expiresIn = _jwtOptions.ExpiryMinutes;
        var ExpirationDate = DateTime.UtcNow.AddMinutes(expiresIn);

        var Token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: ExpirationDate,
            signingCredentials: SigningCredintials
            );

        return (Token: new JwtSecurityTokenHandler().WriteToken(Token), ExpiresIn: expiresIn * 60);
    }

    public Result<string> ValidateToken(string Token, bool validateLifetime = true)
    {
        var TokenHandler = new JwtSecurityTokenHandler();
        var SynmmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));

        try
        {
            TokenHandler.ValidateToken(Token, new TokenValidationParameters
            {
                IssuerSigningKey = SynmmetricKey,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            var userId = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;

            return Result.Success(userId);

        }
        catch
        {
            return Result.Failure<string>(UserErrors.InvalidJwtToken);
        }
    }
}
