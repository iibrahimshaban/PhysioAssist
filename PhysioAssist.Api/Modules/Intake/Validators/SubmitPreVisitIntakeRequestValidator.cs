using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

namespace PhysioAssist.Api.Modules.Intake.Validators;

public class SubmitPreVisitIntakeRequestValidator : AbstractValidator<SubmitPreVisitIntakeRequest>
{
    public SubmitPreVisitIntakeRequestValidator()
    {
        RuleFor(x => x.PatientName)
            .NotEmpty()
            .WithMessage("Patient name is required.")
            .MaximumLength(200)
            .WithMessage("Patient name cannot exceed 200 characters.");

        RuleFor(x => x.PatientEmail)
            .EmailAddress()
            .WithMessage("Invalid email address.")
            .MaximumLength(200)
            .WithMessage("Patient email cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.PatientEmail));

        RuleFor(x => x.PatientPhone)
            .MaximumLength(20)
            .WithMessage("Patient phone cannot exceed 20 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.PatientPhone));

        RuleFor(x => x.FormSubmissionData)
            .NotEmpty()
            .WithMessage("Form submission data is required.");
    }
}
