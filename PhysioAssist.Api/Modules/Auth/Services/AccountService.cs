using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhysioAssist.Api.Modules.Auth.Contracts.Account;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Persistence;

namespace PhysioAssist.Api.Modules.Auth.Services;

public class AccountService(
    UserManager<ApplicationUser> userManager,
    IMediaStorageService mediaStorageService,
    ApplicationDbContext context) : IAccountService   // <-- swap AuthDbContext for your real name
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IMediaStorageService _mediaStorageService = mediaStorageService;
    private readonly ApplicationDbContext _context = context;

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault();
            return Result.Failure(new Error(
                error?.Code ?? "User.ChangePasswordFailed",
                error?.Description ?? "Unable to change password.",
                StatusCodes.Status400BadRequest));
        }

        var activeTokens = user.RefreshTokens.Where(x => x.IsActive).ToList();
        foreach (var token in activeTokens)
            token.RevokedOn = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result<UserProfileResponse>> GetProfileAsync(string userId)
    {
        var user = await _userManager.Users
            .Where(x => x.Id == userId)
            .FirstOrDefaultAsync();

        if (user is null)
            return Result.Failure<UserProfileResponse>(UserErrors.UserNotFound);

        var doctor = await _context.Set<Doctor>()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var response = new UserProfileResponse(
            user.Email!,
            user.FirstName,
            user.LastName,
            user.UserName!,
            user.PhoneNumber,
            user.ProfilePictureUrl,
            doctor?.Title,
            doctor?.ClinicName,
            doctor?.ClinicAddress,
            doctor?.About,
            doctor?.YearsOfExperience,
            CalculateCompletion(user, doctor)
        );

        return Result.Success(response);
    }

    public async Task<Result> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var currentUser = await _userManager.FindByIdAsync(userId);

        if (currentUser is null)
            return Result.Failure(UserErrors.UserNotFound);

        var userNameExists = await _userManager.Users
            .AnyAsync(x => x.UserName == request.UserName && x.Id != userId, cancellationToken);

        if (userNameExists)
            return Result.Failure(UserErrors.DoublicatedUserName);

        var photoUrl = currentUser.ProfilePictureUrl;

        if (request.ProfilePhoto is not null)
        {
            if (!string.IsNullOrEmpty(currentUser.ProfilePictureUrl))
                await _mediaStorageService.DeleteImageByUrlAsync(currentUser.ProfilePictureUrl);

            photoUrl = await _mediaStorageService.UploadImageAsync(
                request.ProfilePhoto, "profile-photos", userId);
        }
        else if (request.RemoveProfilePhoto)
        {
            if (!string.IsNullOrEmpty(currentUser.ProfilePictureUrl))
                await _mediaStorageService.DeleteImageByUrlAsync(currentUser.ProfilePictureUrl);

            photoUrl = null;
        }

        await _userManager.Users
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(setter =>
                 setter
                 .SetProperty(x => x.FirstName, request.FirstName)
                 .SetProperty(x => x.LastName, request.LastName)
                 .SetProperty(x => x.UserName, request.UserName)
                 .SetProperty(x => x.PhoneNumber, request.PhoneNumber)
                 .SetProperty(x => x.ProfilePictureUrl, photoUrl),
                 cancellationToken
            );

        // --- Update Doctor (create if it doesn't exist yet) ---
        var doctor = await _context.Set<Doctor>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (doctor is null)
        {
            doctor = new Doctor { UserId = userId };
            _context.Set<Doctor>().Add(doctor);
        }

        doctor.Title = request.Title;
        doctor.ClinicName = request.ClinicName ?? doctor.ClinicName;
        doctor.ClinicAddress = request.ClinicAddress;
        doctor.About = request.About;
        doctor.YearsOfExperience = request.YearsOfExperience;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static int CalculateCompletion(ApplicationUser user, Doctor? doctor)
    {
        var fields = new List<bool>
        {
            !string.IsNullOrWhiteSpace(user.FirstName),
            !string.IsNullOrWhiteSpace(user.LastName),
            !string.IsNullOrWhiteSpace(user.PhoneNumber),
            !string.IsNullOrWhiteSpace(user.ProfilePictureUrl),
            !string.IsNullOrWhiteSpace(doctor?.Title),
            !string.IsNullOrWhiteSpace(doctor?.ClinicName),
            !string.IsNullOrWhiteSpace(doctor?.ClinicAddress),
            !string.IsNullOrWhiteSpace(doctor?.About),
            doctor?.YearsOfExperience is not null,
        };

        var completed = fields.Count(x => x);
        return (int)Math.Round(completed * 100.0 / fields.Count);
    }
}