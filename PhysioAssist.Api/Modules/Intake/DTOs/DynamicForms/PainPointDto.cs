namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record PainPointDto
{
    public string BodyPart { get; init; } = string.Empty;
    public double X { get; init; }
    public double Y { get; init; }
    public int Intensity { get; init; }
    public string? Description { get; init; }
}
