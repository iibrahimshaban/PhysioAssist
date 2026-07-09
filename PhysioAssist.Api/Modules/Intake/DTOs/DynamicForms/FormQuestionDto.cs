namespace PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

public record FormQuestionDto
{
    public string QuestionId { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool Required { get; init; }
    public int Order { get; init; }
    public string? Placeholder { get; init; }
    public string? HelpText { get; init; }
    public List<string>? Options { get; init; }
    public List<ValidationRuleDto>? ValidationRules { get; init; }
    public List<QuestionConditionDto>? Conditions { get; init; }
}
