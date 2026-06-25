using PhysioAssist.Api.Abstraction.Consts;

namespace PhysioAssist.Api.Contracts.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("password is required")
            .Matches(RegexPattern.Password)
            .WithMessage("Password should be at least 8 digits and should contains Lowercase, NonAlphanumeric and Uppercase");

    }
}
