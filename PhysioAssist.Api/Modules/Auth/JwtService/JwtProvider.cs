using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Shared.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace PhysioAssist.Api.Modules.Auth.JwtService;

public class JwtProvider(IOptions<JwtOptions> jwtOptions, IOptions<GoogleOptions> options) : IJwtProvider
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly GoogleOptions _googleOptions = options.Value;

    public (string Token, int ExpiresIn) GenerateToken(ApplicationUser user
        , IEnumerable<string> Roles, IEnumerable<string> Permissions)
    {
        List<Claim> claims = [
            new(JwtRegisteredClaimNames.Sub,user.Id),
                new(JwtRegisteredClaimNames.Email,user.Email!),
                new(JwtRegisteredClaimNames.GivenName,user.FirstName),
                new(JwtRegisteredClaimNames.FamilyName,user.LastName),
                new(JwtRegisteredClaimNames.Jti,Guid.CreateVersion7().ToString()),
                new(nameof(Roles),JsonSerializer.Serialize(Roles),JsonClaimValueTypes.JsonArray),
                new(nameof(Permissions),JsonSerializer.Serialize(Permissions),JsonClaimValueTypes.JsonArray)
            ];

        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            claims.Add(new Claim("profilePictureUrl", user.ProfilePictureUrl));
        }

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
    public string GenerateGoogleOnboardingTicket(string googleSubject, string email, string? pictureUrl)
    {
        Claim[] claims = [
            new(JwtRegisteredClaimNames.Sub, googleSubject),
            new(JwtRegisteredClaimNames.Email, email),
            new("picture", pictureUrl ?? string.Empty),
            new("purpose", _googleOptions.GoogleOnboardingPurpose)
        ];

        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_googleOptions.OnboardingTicketExpiryMinutes)),
            signingCredentials: signingCredentials
            );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Result<(string GoogleSubject, string Email, string? PictureUrl)> ValidateGoogleOnboardingTicket(string ticket)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));

        try
        {
            tokenHandler.ValidateToken(ticket, new TokenValidationParameters
            {
                IssuerSigningKey = symmetricKey,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            var purpose = jwtToken.Claims.FirstOrDefault(x => x.Type == "purpose")?.Value;
            if (purpose !=  _googleOptions.GoogleOnboardingPurpose)
                return Result.Failure<(string, string, string?)>(UserErrors.InvalidOrExpiredOnboardingTicket);

            var googleSubject = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
            var email = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Email).Value;
            var pictureUrl = jwtToken.Claims.FirstOrDefault(x => x.Type == "picture")?.Value;

            return Result.Success((googleSubject, email, pictureUrl));
        }
        catch
        {
            return Result.Failure<(string, string, string?)>(UserErrors.InvalidOrExpiredOnboardingTicket);
        }
    }
}
