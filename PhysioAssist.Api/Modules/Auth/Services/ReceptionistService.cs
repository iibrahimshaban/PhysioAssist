using Microsoft.AspNetCore.Identity;
using PhysioAssist.Api.Modules.Auth.Contracts.Receptionist;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Persistence;
using System.Security.Claims;

namespace PhysioAssist.Api.Modules.Auth.Services;

public class ReceptionistService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext) : IReceptionistService
{
    public async Task<Result<ReceptionistResponse>> CreateAsync(Guid doctorId, CreateReceptionistRequest request, CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return Result.Failure<ReceptionistResponse>(ReceptionistErrors.EmailTaken);

        Guid userId = Guid.CreateVersion7();

        var user = new ApplicationUser
        {
            Id = userId.ToString(),
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.Phone,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return Result.Failure<ReceptionistResponse>(
                ReceptionistErrors.CreateFailed(string.Join(", ", createResult.Errors.Select(e => e.Description))));

        await userManager.AddToRoleAsync(user, DefaultRoles.Receptionist);

        var validPermissions = request.Permissions
            .Intersect(Permissions.GetAllPermissions())
            .ToList();

        if (validPermissions.Count != 0)
            await userManager.AddClaimsAsync(user,
                validPermissions.Select(p => new Claim(Permissions.Type, p!)));

        var receptionist = new Receptionist
        {
            Id = userId,
            ManagingDoctorId = doctorId,
            UserId = user.Id,
            Shift = request.Shift,
            From = request.From,
            To = request.To
        };

        dbContext.Receptionists.Add(receptionist);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToResponse(receptionist, user, validPermissions!));
    }

    public async Task<Result<IEnumerable<ReceptionistResponse>>> GetAllAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var receptionists = await dbContext.Receptionists
            .Include(r => r.User)
            .Where(r => r.ManagingDoctorId == doctorId)
            .ToListAsync();

        var responses = new List<ReceptionistResponse>();
        foreach (var r in receptionists)
        {
            var claims = await userManager.GetClaimsAsync(r.User);
            responses.Add(MapToResponse(r, r.User,
                claims.Where(c => c.Type == Permissions.Type).Select(c => c.Value).ToList()));
        }

        return Result.Success<IEnumerable<ReceptionistResponse>>(responses);
    }

    public async Task<Result<ReceptionistResponse>> GetByIdAsync(Guid receptionistId, CancellationToken cancellationToken = default)
    {
        var receptionist = await dbContext.Receptionists
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == receptionistId);

        if (receptionist is null)
            return Result.Failure<ReceptionistResponse>(ReceptionistErrors.NotFound);

        var claims = await userManager.GetClaimsAsync(receptionist.User);
        return Result.Success(MapToResponse(receptionist, receptionist.User,
            claims.Where(c => c.Type == Permissions.Type).Select(c => c.Value).ToList()));
    }

    public async Task<Result<ReceptionistResponse>> UpdateAsync(Guid receptionistId, UpdateReceptionistRequest request, CancellationToken cancellationToken = default)
    {
        var receptionist = await dbContext.Receptionists
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == receptionistId);

        if (receptionist is null)
            return Result.Failure<ReceptionistResponse>(ReceptionistErrors.NotFound);

        receptionist.User.FirstName = request.FirstName;
        receptionist.User.LastName = request.LastName;
        receptionist.User.PhoneNumber = request.Phone;
        receptionist.Shift = request.Shift;
        receptionist.From = request.From;
        receptionist.To = request.To;

        var currentClaims = (await userManager.GetClaimsAsync(receptionist.User))
            .Where(c => c.Type == Permissions.Type).ToList();

        var currentValues = currentClaims.Select(c => c.Value).ToHashSet();
        var newValues = request.Permissions.Intersect(Permissions.GetAllPermissions()!).ToHashSet();

        var toRemove = currentClaims.Where(c => !newValues.Contains(c.Value)).ToList();
        var toAdd = newValues.Except(currentValues).Select(p => new Claim(Permissions.Type, p)).ToList();

        if (toRemove.Count != 0) await userManager.RemoveClaimsAsync(receptionist.User, toRemove);
        if (toAdd.Count != 0) await userManager.AddClaimsAsync(receptionist.User, toAdd);

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(receptionist.User);
            await userManager.ResetPasswordAsync(receptionist.User, token, request.NewPassword);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToResponse(receptionist, receptionist.User, [.. newValues]));
    }

    public async Task<Result> ToggleDisabledAsync(Guid receptionistId, CancellationToken cancellationToken = default)
    {
        var receptionist = await dbContext.Receptionists
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == receptionistId);

        if (receptionist is null)
            return Result.Failure(ReceptionistErrors.NotFound);

        receptionist.User.IsDisabled = !receptionist.User.IsDisabled;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid receptionistId, CancellationToken cancellationToken = default)
    {
        var receptionist = await dbContext.Receptionists
        .Include(r => r.User)
        .FirstOrDefaultAsync(r => r.Id == receptionistId, cancellationToken);

        if (receptionist is null)
            return Result.Failure(ReceptionistErrors.NotFound);

        var user = receptionist.User;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Receptionists.Remove(receptionist);
        await dbContext.SaveChangesAsync(cancellationToken);

        var deleteResult = await userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(
                ReceptionistErrors.CreateFailed(string.Join(", ", deleteResult.Errors.Select(e => e.Description))));
        }

        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
    private static ReceptionistResponse MapToResponse(Receptionist r, ApplicationUser user, List<string> permissions) =>
        new(r.Id, $"{user.FirstName} {user.LastName}", user.Email!, user.PhoneNumber!,
            !user.IsDisabled, r.Shift, r.From, r.To, r.ManagingDoctorId, permissions,
            string.IsNullOrEmpty(user.ProfilePictureUrl) ? null : user.ProfilePictureUrl);

    
}
