namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public record CompleteGoogleOnboardingRequest(
    string OnboardingToken,
    string FirstName,
    string LastName,
    string ClinicName,
    IFormFile? ProfilePhoto
);
