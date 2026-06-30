namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public class ResendConfirmEmailRequestValidator : AbstractValidator<ResendConfirmEmailRequest>
{
    public ResendConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("email filed is required")
            .EmailAddress().WithMessage("please enter a valid email ");
    }
}
