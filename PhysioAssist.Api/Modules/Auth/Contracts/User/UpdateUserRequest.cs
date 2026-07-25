namespace PhysioAssist.Api.Modules.Auth.Contracts.User;

public record UpdateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string UserName,
    IList<string> Roles
    );
