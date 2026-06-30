namespace PhysioAssist.Api.Modules.Auth.Contracts.User;

public record UserResponse(
    string Id,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    bool IsDisabled,
    IEnumerable<string> Roles
    );
