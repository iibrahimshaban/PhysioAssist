namespace PhysioAssist.Api.Modules.Auth.Contracts.User;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName)
           .NotEmpty().WithMessage("Username is required.")
           .Matches(RegexPattern.UserName).WithMessage("Username can only have letters and numbers")
           .Length(3, 100).WithMessage("username must be greatter then 3 chars and less thean 100 char");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 100).WithMessage("first name must be greatter then 3 chars and less thean 100 char");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 100).WithMessage("last name must be greatter then 3 chars and less thean 100 char");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("password is required")
            .Matches(RegexPattern.Password)
            .WithMessage("Password should be at least 8 digits and should contains Lowercase, NonAlphanumeric and Uppercase");

        RuleFor(x => x.Roles)
           .NotEmpty().NotNull().WithMessage("user need at least one role");

        RuleFor(x => x.Roles)
            .Must(x => x.Distinct().Count() == x.Count()).WithMessage("doblicated role for the same user")
            .When(x => x.Roles is not null);
    }
}

