namespace PhysioAssist.Api.Modules.DocumentationModule.Contracts;

public class GenerateDocumentationSummaryRequestValidator : AbstractValidator<GenerateDocumentationSummaryRequest>
{
    public GenerateDocumentationSummaryRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Audience).IsInEnum();

        When(x => x.Audience == SummaryAudience.Colleague, () =>
        {
            RuleFor(x => x.Scope)
                .NotNull()
                .WithMessage("Scope is required when generating a summary for a colleague.");
        });

        When(x => x.Scope == SummaryScope.Focused, () =>
        {
            RuleFor(x => x.FocusAreas)
                .NotNull()
                .Must(f => f is { Count: > 0 })
                .WithMessage("FocusAreas is required when Scope is Focused.");
        });
    }
}
