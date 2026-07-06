using PhysioAssist.Api.Modules.Auth.Contracts.Account;

namespace PhysioAssist.Api.Modules.Auth.Services;

public interface IAccountService
{
    Task<Result<UserProfileResponse>> GetProfileAsync(string userId);
    Task<Result> UpdateProfileAsync(string UserId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(string UserId, ChangePasswordRequest request);
}
