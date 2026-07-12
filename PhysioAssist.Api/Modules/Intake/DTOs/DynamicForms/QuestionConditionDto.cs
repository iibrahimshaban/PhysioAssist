namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record QuestionConditionDto
{
    public string TargetQuestionId { get; init; } = string.Empty;
    public string Operator { get; init; } = string.Empty;
    public string? Value { get; init; }
}
