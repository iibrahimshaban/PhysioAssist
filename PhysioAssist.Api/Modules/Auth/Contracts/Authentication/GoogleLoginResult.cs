namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public class GoogleLoginResult
{
    public bool RequiresOnboarding { get; }
    public AuthResponse? Tokens { get; }
    public GoogleOnboardingRequiredResponse? Onboarding { get; }

    private GoogleLoginResult(AuthResponse? tokens, GoogleOnboardingRequiredResponse? onboarding, bool requiresOnboarding)
    {
        Tokens = tokens;
        Onboarding = onboarding;
        RequiresOnboarding = requiresOnboarding;
    }

    public static GoogleLoginResult LoggedIn(AuthResponse tokens) => new(tokens, null, false);

    public static GoogleLoginResult NeedsOnboarding(GoogleOnboardingRequiredResponse onboarding) => new(null, onboarding, true);
}
