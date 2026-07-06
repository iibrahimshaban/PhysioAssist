namespace PhysioAssist.Api.Modules.Auth.Contracts.Account;

public record UpdateProfileRequest(
    string UserName,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Title,
    string? ClinicName,
    string? ClinicAddress,
    string? About,
    int? YearsOfExperience,
    IFormFile? ProfilePhoto,
    bool RemoveProfilePhoto = false
);
