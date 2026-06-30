using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Contracts.Authentication;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Modules.Auth.Services;

namespace PhysioAssist.Api.Modules.Auth.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("registration")]
    public async Task<IActionResult> Registration([FromForm] RegistrationRequest request)
    {
        var result = await _authService.RegistrationAsync(request);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var result = await _authService.ConfirmEmailAsync(request);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
    [HttpPost("resend-confirmation-email")]
    public async Task<IActionResult> ResendConfiramtionEmail([FromBody] ResendConfirmEmailRequest request)
    {
        var result = await _authService.ResendEmailConfirmationCode(request);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
    [HttpPost("new-refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.GetRefreshTokenAsync(request);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
    [HttpPost("revoke-refresh-token")]
    public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RevokeRefreshTokenAsync(request);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
    [HttpPost("forget-passowrd")]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest request)
    {
        var result = await _authService.SendResetPasswordCodeAsync(request);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
