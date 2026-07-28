using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

namespace PhysioAssist.Api.Modules.Intake.Validators;

public class ConvertIntakeToPatientRequestValidator : AbstractValidator<ConvertIntakeToPatientRequest>
{
    public ConvertIntakeToPatientRequestValidator()
    {
        RuleFor(x => x.FormSubmissionData)
            .MaximumLength(100_000)
            .WithMessage("Form submission data cannot exceed 100,000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.FormSubmissionData));

        RuleFor(x => x.PainPointsData)
            .MaximumLength(100_000)
            .WithMessage("Pain points data cannot exceed 100,000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.PainPointsData));
    }
}
