using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Shared.Options;

public class GoogleOptions
{
    public const string SectionName = "GoogleOptions";

    public  string ClientId { get; set; } = string.Empty;
    public  string ClientSecret { get; set; } = string.Empty;
    [Required]
    public string GoogleOnboardingPurpose {  get; set; } = string.Empty;
    [Required]
    public string OnboardingTicketExpiryMinutes {  get; set; } = string.Empty;
}
