using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

namespace PhysioAssist.Api.Modules.Intake.Validators;

public class SubmitPreVisitIntakeRequestValidator : AbstractValidator<SubmitPreVisitIntakeRequest>
{
    public SubmitPreVisitIntakeRequestValidator()
    {

        RuleFor(x => x.FormSubmissionData)
            .NotEmpty()
            .WithMessage("Form submission data is required.");
    }
}
