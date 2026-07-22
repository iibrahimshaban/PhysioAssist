namespace PhysioAssist.Api.Modules.Auth.Contracts.Receptionist;

public class UpdateReceptionistRequestValidator : AbstractValidator<UpdateReceptionistRequest>
{
    public UpdateReceptionistRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(RegexPattern.PhoneNumber);

        RuleFor(x => x.Shift)
            .IsInEnum();

        RuleFor(x => x.To)
            .GreaterThan(x => x.From)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("Shift end time must be after start time.");

        RuleFor(x => x.Permissions)
            .NotNull();

        RuleForEach(x => x.Permissions)
            .Must(p => Permissions.GetAllPermissions().Contains(p))
            .WithMessage("'{PropertyValue}' is not a recognized permission.");

        RuleFor(x => x.NewPassword)
            .Matches(RegexPattern.Password)
            .When(x => !string.IsNullOrEmpty(x.NewPassword))
            .WithMessage("Password should be at least 8 characters and should contain a lowercase letter, an uppercase letter, and a non-alphanumeric character.");
    }
}
