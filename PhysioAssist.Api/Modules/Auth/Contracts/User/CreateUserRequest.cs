namespace PhysioAssist.Api.Modules.Auth.Contracts.User;

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string UserName,
    IList<string> Roles,
    Guid? ManagingDoctorId = null
    );
