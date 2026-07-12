namespace PhysioAssist.Api.Modules.DocumentationModule.Contracts;

public sealed class GenerateDocumentationSummaryRequest
{
    public Guid PatientId { get; set; }
    public SummaryAudience Audience { get; set; }
    public SummaryScope? Scope { get; set; }
    public List<string>? FocusAreas { get; set; }
}
