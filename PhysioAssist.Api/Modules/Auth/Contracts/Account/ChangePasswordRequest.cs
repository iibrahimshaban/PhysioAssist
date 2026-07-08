namespace PhysioAssist.Api.Modules.Auth.Contracts.Account;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
    );
