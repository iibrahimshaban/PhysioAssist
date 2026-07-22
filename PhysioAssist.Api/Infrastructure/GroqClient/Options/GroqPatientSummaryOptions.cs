using System.ComponentModel.DataAnnotations;

namespace PhysioAssist.Api.Infrastructure.GroqClient.Options;

public class GroqPatientSummaryOptions
{
    public const string SectionName = "GroqPatientSummary";

    [Required]
    public string Endpoint { get; set; }
    public string Token { get; set; } = string.Empty;
    [Required]
    public string ChatModel { get; set; }
}
