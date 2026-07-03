namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public record VerifyResetOtpRequest(
    string Email, 
    string Otp
    );
