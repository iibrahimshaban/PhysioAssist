namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public record LoginRequest(
    string Email,
    string Password
    );
