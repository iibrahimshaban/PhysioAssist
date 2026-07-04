using Hangfire;
using Mapster;
using Microsoft.AspNetCore.Identity;
using PhysioAssist.Api.Modules.Auth.Contracts.Authentication;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Modules.Auth.JwtService;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Consts;
using PhysioAssist.Api.Shared.Helpers;
using PhysioAssist.Api.Shared.Interfaces;
using System.Security.Cryptography;

namespace PhysioAssist.Api.Modules.Auth.Services;

public class AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtProvider jwtProvider,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        ICustomEmailService emailService,
        IMediaStorageService storageService
        ) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtProvider _jwtToken = jwtProvider;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly ApplicationDbContext _context = context;
    private readonly ICustomEmailService _emailService = emailService;
    private readonly IMediaStorageService _storageService= storageService;

    private static readonly int RefreshTokenExpiryInDays = 90;
    private const int OtpExpiryIn = 15;

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        if (user.IsDisabled)
            return Result.Failure<AuthResponse>(UserErrors.DisabledUser);

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, true);

        if (result.Succeeded)
        {
            (var Roles, var Permissions) = await GetUserRolesAndPermissions(user);
            (var token, var expiresIn) = _jwtToken.GenerateToken(user, Roles, Permissions);

            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiryDate = DateTime.UtcNow.AddDays(RefreshTokenExpiryInDays);

            user.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                ExpiresOn = refreshTokenExpiryDate
            });

            await _userManager.UpdateAsync(user);
            return Result.Success(new AuthResponse(user.Id,  user.FirstName, user.LastName, user.Email!, user.UserName!,
                token, expiresIn,refreshToken, refreshTokenExpiryDate, user.ProfilePictureUrl));
        }

        var error = result.IsNotAllowed ? UserErrors.EmailNotConfirmed
                    : result.IsLockedOut ? UserErrors.LockedUser
                    : UserErrors.InvalidCredentials;

        return Result.Failure<AuthResponse>(error);
    }

    public async Task<Result> RegistrationAsync(RegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var userId = Guid.CreateVersion7();

        var user = new ApplicationUser
        {
            Id = userId.ToString(),
            Email = request.Email,
            UserName = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsDisabled = false,
            ProfilePictureUrl = string.Empty
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault();
            return error is null
                ? Result.Failure(UserErrors.RegistrationFailed)
                : Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        if (request.ProfilePhoto is not null)
        {
            user.ProfilePictureUrl = await _storageService.UploadImageAsync(request.ProfilePhoto, "users", userId.ToString());
            await _userManager.UpdateAsync(user);
        }

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        await _context.Doctors.AddAsync(new Doctor
        {
            UserId = userId.ToString(),
            ClinicName = request.ClinicName,
        }, cancellationToken);

        await _context.OtpEntries.AddAsync(new OtpEntry
        {
            Code = code,
            UserId = userId.ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryIn),
            Purpose = OtpPurpose.EmailVerification
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var html = EmailBodyBuilder.EmailConfirmation(user.FirstName, code);

        BackgroundJob.Enqueue(() =>
            _emailService.SendEmailAsync(user.Email, "Verify your PhysioAssist email", html)
        );

        return Result.Success();
    }

    public async Task<Result<AuthResponse>> GetRefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellation=default)
    {
        var result = _jwtToken.ValidateToken(request.Token, validateLifetime: false);

        if (result.IsFailure)
            return Result.Failure<AuthResponse>(UserErrors.InvalidJwtToken);

        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == result.Value, cancellation);

        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidJwtToken);

        if (user.IsDisabled)
            return Result.Failure<AuthResponse>(UserErrors.DisabledUser);

        if (user.LockoutEnd > DateTime.UtcNow)
            return Result.Failure<AuthResponse>(UserErrors.LockedUser);

        var userRefreshToken = user.RefreshTokens.SingleOrDefault(x => x.Token == request.RefreshToken && x.IsActive);

        if (userRefreshToken == null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidRefresh);

        userRefreshToken.RevokedOn = DateTime.UtcNow;

        (var Roles, var Permissions) = await GetUserRolesAndPermissions(user);
        (var NewToken, var ExpiryIn) = _jwtToken.GenerateToken(user, Roles,Permissions);

        var NewRefreshToken = GenerateRefreshToken();
        var RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(RefreshTokenExpiryInDays);

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = NewRefreshToken,
            ExpiresOn = RefreshTokenExpiryDate,
        });

        await _userManager.UpdateAsync(user);

        var response = new AuthResponse(user.Id, user.FirstName, user.LastName, user.Email!, user.UserName!
        , NewToken, ExpiryIn, NewRefreshToken, RefreshTokenExpiryDate,null);

        return Result.Success(response);
    }

    public async Task<Result> RevokeRefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellation = default)
    {
        var result = _jwtToken.ValidateToken(request.Token, validateLifetime:false);

        if (result.IsFailure)
            return Result.Failure(UserErrors.InvalidJwtToken);

        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == result.Value, cancellation);

        if (user is null)
            return Result.Failure(UserErrors.InvalidJwtToken);

        if (user.IsDisabled)
            return Result.Failure(UserErrors.DisabledUser);

        if (user.LockoutEnd > DateTime.UtcNow)
            return Result.Failure(UserErrors.LockedUser);

        var userRefreshToken = user.RefreshTokens.SingleOrDefault(x => x.Token == request.RefreshToken && x.IsActive);

        if (userRefreshToken == null)
            return Result.Failure(UserErrors.InvalidRefresh);

        userRefreshToken.RevokedOn = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        return Result.Success();
    }
    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Result.Failure(UserErrors.InvalidCode);

        if (user.EmailConfirmed)
            return Result.Failure(UserErrors.DuplicatedConfirmation);

        var otp = await _context.OtpEntries
            .Where(x => x.UserId == user.Id
                     && x.Purpose == OtpPurpose.EmailVerification
                     && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || otp.IsExpired)
            return Result.Failure(UserErrors.InvalidCode);

        if (request.Code != otp.Code)
            return Result.Failure(UserErrors.InvalidCode);

        otp.IsUsed = true;
        await _context.SaveChangesAsync(cancellationToken);


        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
        await _userManager.AddToRoleAsync(user, DefaultRoles.SoloDoctor);

        return Result.Success();
    }

    public async Task<Result> ResendEmailConfirmationCode(ResendConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        
        if (await _userManager.FindByEmailAsync(request.Email) is not { } user)
            return Result.Success();

        if (user.EmailConfirmed)
            return Result.Failure(UserErrors.DuplicatedConfirmation);


        var newOtp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        await _context.OtpEntries
            .Where(x => x.UserId == user.Id && x.Purpose == OtpPurpose.EmailVerification && !x.IsUsed)
            .ExecuteUpdateAsync(setter =>
                setter.SetProperty(x => x.IsUsed, true), cancellationToken);

        await _context.OtpEntries.AddAsync(new OtpEntry
        {
            Code = newOtp,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryIn),
            Purpose = OtpPurpose.EmailVerification
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var html = EmailBodyBuilder.EmailConfirmation(user.FirstName, newOtp);

        BackgroundJob.Enqueue(() =>
         _emailService.SendEmailAsync(user.Email!, "Verify your PhysioAssist email", html)
        );

        return Result.Success();
    }

    public async Task<Result> SendResetPasswordCodeAsync(ForgetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not { } user)
            return Result.Success();

        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        var otpEntry = new OtpEntry
        {
            UserId = user.Id,
            Code = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryIn),
            Purpose = OtpPurpose.PasswordReset
        };

        await _context.AddAsync(otpEntry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var html = EmailBodyBuilder.PasswordReset(user.FirstName, otpCode);

        BackgroundJob.Enqueue(() =>
            _emailService.SendEmailAsync(user.Email!, "Reset your PhysioAssist password", html)
        );

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Result.Failure(UserErrors.InvalidCode);

        var otp = await _context.OtpEntries
            .Where(x => x.UserId == user.Id
                     && x.Purpose == OtpPurpose.PasswordReset
                     && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || otp.IsExpired)
            return Result.Failure(UserErrors.InvalidCode);

        if (otp.Code != request.Otp)
            return Result.Failure(UserErrors.InvalidCode);

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        if (!resetResult.Succeeded)
        {
            var error = resetResult.Errors.FirstOrDefault();
            return error is null
                ? Result.Failure(UserErrors.ResetPasswordFailed)
                : Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        otp.IsUsed = true;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
    public async Task<Result> VerifyResetOtpAsync(VerifyResetOtpRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return Result.Failure(UserErrors.InvalidCode);

        var otp = await _context.OtpEntries
            .Where(x => x.UserId == user.Id
                     && x.Purpose == OtpPurpose.PasswordReset
                     && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || otp.IsExpired || otp.Code != request.Otp)
            return Result.Failure(UserErrors.InvalidCode);

        return Result.Success();
    }
    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
    private async Task<(IEnumerable<string> Roles, IEnumerable<string> Permissions)> GetUserRolesAndPermissions(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var permissions = await (from r in _context.Roles
                                 join rc in _context.RoleClaims
                                 on r.Id equals rc.RoleId
                                 where roles.Contains(r.Name!)
                                 select rc.ClaimValue)
                                 .Distinct()
                                 .ToListAsync();

        return (roles, permissions);
    }

    
}

