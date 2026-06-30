namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public record ResetPasswordRequest(
    string Email,
    string NewPassword,
    string Otp
    );
