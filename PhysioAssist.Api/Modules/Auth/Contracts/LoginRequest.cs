namespace PhysioAssist.Api.Modules.Auth.Contracts;

public record LoginRequest(
    string Email,
    string Password
    );
