using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;

namespace PhysioAssist.Api.Modules.Intake.Validators;

public class CreateFormSchemaRequestValidator : AbstractValidator<CreateFormSchemaRequest>
{
    public CreateFormSchemaRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Schema name is required.")
            .MaximumLength(150)
            .WithMessage("Schema name cannot exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.SchemaJson)
            .NotEmpty()
            .WithMessage("Schema JSON is required.");
    }
}
