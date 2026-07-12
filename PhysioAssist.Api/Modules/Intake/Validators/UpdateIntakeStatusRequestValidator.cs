using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;

namespace PhysioAssist.Api.Modules.Intake.Validators;

public class UpdateIntakeStatusRequestValidator : AbstractValidator<UpdateIntakeStatusRequest>
{
    public UpdateIntakeStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("Invalid intake status value.");
    }
}
