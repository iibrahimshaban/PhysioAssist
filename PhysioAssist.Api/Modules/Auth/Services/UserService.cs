using Mapster;
using Microsoft.AspNetCore.Identity;
using PhysioAssist.Api.Modules.Auth.Contracts.User;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Persistence;

namespace PhysioAssist.Api.Modules.Auth.Services;

public class UserService(ApplicationDbContext _context, UserManager<ApplicationUser> _userManager) : IUserService
{
    public async Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var flatRows = await (from u in _context.Users
                              join ur in _context.UserRoles on u.Id equals ur.UserId into urJoin
                              from ur in urJoin.DefaultIfEmpty()
                              join r in _context.Roles on ur.RoleId equals r.Id into rJoin
                              from r in rJoin.DefaultIfEmpty()
                              select new
                              {
                                  u.Id,
                                  u.Email,
                                  u.UserName,
                                  u.FirstName,
                                  u.LastName,
                                  u.IsDisabled,
                                  RoleName = r != null ? r.Name : null
                              })
                              .ToListAsync(cancellationToken);

        var users = flatRows
            .GroupBy(x => new { x.Id, x.Email, x.UserName, x.FirstName, x.LastName, x.IsDisabled })
            .Where(g => !g.Any(x => x.RoleName == DefaultRoles.Admin))
            .Select(g => new UserResponse(
                g.Key.Id,
                g.Key.Email,
                g.Key.UserName,
                g.Key.FirstName,
                g.Key.LastName,
                g.Key.IsDisabled,
                g.Where(x => x.RoleName is not null).Select(x => x.RoleName!)
            ))
            .ToList();

        return users;
    }
    public async Task<Result<UserResponse>> GetDetailsAsync(string UserId, CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByIdAsync(UserId) is not { } user)
            return Result.Failure<UserResponse>(UserErrors.UserNotFound);

        var roles = await _userManager.GetRolesAsync(user);

        var response = (user, roles).Adapt<UserResponse>();

        return Result.Success(response);
    }
    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
{
    var emailExists = await _userManager.Users.AnyAsync(x => x.Email == request.Email, cancellationToken);
    if (emailExists)
        return Result.Failure<UserResponse>(UserErrors.DuplicatedEmail);

    var userNameExists = await _userManager.Users.AnyAsync(x => x.UserName == request.UserName, cancellationToken);
    if (userNameExists)
        return Result.Failure<UserResponse>(UserErrors.DoublicatedUserName);

    var currentRoles = await _context.Roles
        .Select(x => x.Name!)
        .AsNoTracking()
        .ToListAsync(cancellationToken);

    if (request.Roles.Except(currentRoles).Any())
        return Result.Failure<UserResponse>(UserErrors.RoleNotFound);

    var user = request.Adapt<ApplicationUser>();
    user.EmailConfirmed = true;

    await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

    var createResult = await _userManager.CreateAsync(user, request.Password);
    if (!createResult.Succeeded)
    {
        await transaction.RollbackAsync(cancellationToken);
        var createError = createResult.Errors.FirstOrDefault();
        return Result.Failure<UserResponse>(new Error(
            createError?.Code ?? "User.CreateFailed",
            createError?.Description ?? "Failed to create user.",
            StatusCodes.Status400BadRequest));
    }

    var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
    if (!roleResult.Succeeded)
    {
        await transaction.RollbackAsync(cancellationToken);
        var roleError = roleResult.Errors.FirstOrDefault();
        return Result.Failure<UserResponse>(new Error(
            roleError?.Code ?? "User.RoleAssignmentFailed",
            roleError?.Description ?? "Failed to assign roles to user.",
            StatusCodes.Status400BadRequest));
    }

    await transaction.CommitAsync(cancellationToken);

    var response = (user, request.Roles).Adapt<UserResponse>();
    return Result.Success(response);
}
    public async Task<Result> UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByIdAsync(userId) is not { } user)
            return Result.Failure(UserErrors.UserNotFound);

        var EmailExists = await _userManager.Users.AnyAsync(x => x.Email == request.Email && x.Id != userId, cancellationToken);

        if (EmailExists)
            return Result.Failure(UserErrors.DuplicatedEmail);

        var userNameExists = await _userManager.Users.AnyAsync(x => x.UserName == request.UserName && x.Id != userId, cancellationToken);

        if (userNameExists)
            return Result.Failure(UserErrors.DoublicatedUserName);

        var currentRoles = await _context.Roles
            .Select(x => x.Name!)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (request.Roles.Except(currentRoles).Any())
            return Result.Failure(UserErrors.RoleNotFound);

        user = request.Adapt(user);

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await _context.UserRoles
                .Where(x => x.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await _userManager.AddToRolesAsync(user, request.Roles);
            return Result.Success();
        }
        var error = result.Errors.First();

        return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));

    }
    public async Task<Result> ToggleStatusAsync(string userId)
    {
        if (await _userManager.FindByIdAsync(userId) is not { } user)
            return Result.Failure(UserErrors.UserNotFound);

        user.IsDisabled = !user.IsDisabled;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }
    public async Task<Result> UnlockAsync(string userId)
    {
        if (await _userManager.FindByIdAsync(userId) is not { } user)
            return Result.Failure(UserErrors.UserNotFound);

        await _userManager.SetLockoutEndDateAsync(user, null);

        return Result.Success();
    }
}
