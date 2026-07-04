using PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

namespace PhysioAssist.Api.Modules.Auth.Services;

public interface IAuthService
{
    Task<Result> RegistrationAsync(RegistrationRequest request, CancellationToken cancellationToken = default);
    Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResendEmailConfirmationCode(ResendConfirmEmailRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result> RevokeRefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellation=default);
    Task<Result> SendResetPasswordCodeAsync(ForgetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> GetRefreshTokenAsync(RefreshTokenRequest request,CancellationToken cancellation= default);
    Task<Result> VerifyResetOtpAsync(VerifyResetOtpRequest request, CancellationToken cancellationToken = default);
}
