namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public record RegistrationRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string UserName,
    string ClinicName,
    IFormFile? ProfilePhoto
    );
