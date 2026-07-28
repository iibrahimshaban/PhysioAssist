using PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;

namespace PhysioAssist.Api.Modules.Intake.Validators;

public class GenerateIntakeQrLinkRequestValidator : AbstractValidator<GenerateIntakeQrLinkRequest>
{
    public GenerateIntakeQrLinkRequestValidator()
    {
        RuleFor(x => x.ExpiryMonths)
            .GreaterThan(0)
            .WithMessage("Expiry months must be greater than 0.")
            .LessThanOrEqualTo(24)
            .WithMessage("Expiry months cannot exceed 24 (2 years).");
    }
}
