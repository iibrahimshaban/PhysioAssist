using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;

namespace PhysioAssist.Api.Modules.Intake.Validators;

public class PublishFormSchemaRequestValidator : AbstractValidator<PublishFormSchemaRequest>
{
    public PublishFormSchemaRequestValidator()
    {
        RuleFor(x => x.Version)
            .GreaterThan(0)
            .WithMessage("Version must be greater than 0.");
    }
}
