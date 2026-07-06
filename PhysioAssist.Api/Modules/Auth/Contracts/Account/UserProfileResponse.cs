namespace PhysioAssist.Api.Modules.Auth.Contracts.Account;

public record UserProfileResponse(
    string Email,
    string FirstName,
    string LastName,
    string UserName,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    string? Title,
    string? ClinicName,
    string? ClinicAddress,
    string? About,
    int? YearsOfExperience,
    int ProfileCompletionPercentage
);
