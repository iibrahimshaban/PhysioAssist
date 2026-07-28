namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public record GoogleOnboardingRequiredResponse(
    string OnboardingToken, 
    string Email,
    string SuggestedFirstName,
    string SuggestedLastName);

