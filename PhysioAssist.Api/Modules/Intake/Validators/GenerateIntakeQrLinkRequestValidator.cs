using PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;

namespace PhysioAssist.Api.Modules.Intake.Validators;

public class GenerateIntakeQrLinkRequestValidator : AbstractValidator<GenerateIntakeQrLinkRequest>
{
    public GenerateIntakeQrLinkRequestValidator()
    {
        RuleFor(x => x.ExpiryHours)
            .GreaterThan(0)
            .WithMessage("Expiry hours must be greater than 0.")
            .LessThanOrEqualTo(8760)
            .WithMessage("Expiry hours cannot exceed 8760 (1 year).");
    }
}
