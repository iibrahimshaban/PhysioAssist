namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public record RefreshTokenRequest(
    string Token,
    string RefreshToken
    );
