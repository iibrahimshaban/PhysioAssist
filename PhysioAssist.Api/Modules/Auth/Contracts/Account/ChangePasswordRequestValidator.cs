using PhysioAssist.Api.Shared.Consts;

namespace PhysioAssist.Api.Modules.Auth.Contracts.Account;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
           .NotEmpty().WithMessage("password is required")
           .Matches(RegexPattern.Password)
           .WithMessage("Password should be at least 8 digits and should contains Lowercase, NonAlphanumeric and Uppercase");

        RuleFor(x => x.NewPassword)
           .NotEmpty().WithMessage("password is required")
           .Matches(RegexPattern.Password)
           .WithMessage("Password should be at least 8 digits and should contains Lowercase, NonAlphanumeric and Uppercase")
           .NotEqual(x => x.CurrentPassword)
           .WithMessage("New password cannot be same as the current password");
    }
}
