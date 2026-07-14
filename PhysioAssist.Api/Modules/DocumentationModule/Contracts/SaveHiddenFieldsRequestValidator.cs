namespace PhysioAssist.Api.Modules.DocumentationModule.Contracts;

public class SaveHiddenFieldsRequestValidator : AbstractValidator<SaveHiddenFieldsRequest>
{
    public SaveHiddenFieldsRequestValidator()
    {
        RuleFor(x => x.HiddenFieldIds)
            .NotNull();

        RuleForEach(x => x.HiddenFieldIds)
            .NotEmpty()
            .WithMessage("Field id cannot be empty.");
    }
}
