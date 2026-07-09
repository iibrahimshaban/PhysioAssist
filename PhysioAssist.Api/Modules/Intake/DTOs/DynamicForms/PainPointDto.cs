namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record PainPointDto
{
    public string? BodyPart { get; init; }
    public string? AnatomicalRegion { get; init; }
    public string? Side { get; init; }
    public string? SpecificLocation { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public int Intensity { get; init; }
    public string? Description { get; init; }
}
