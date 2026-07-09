using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

namespace PhysioAssist.Api.Modules.Intake.Validators;

public class ConvertIntakeToPatientRequestValidator : AbstractValidator<ConvertIntakeToPatientRequest>
{
    public ConvertIntakeToPatientRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
