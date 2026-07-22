namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record ValidationRuleDto
{
    public string RuleType { get; init; } = string.Empty;
    public string? Value { get; init; }
    public string? Message { get; init; }
}
