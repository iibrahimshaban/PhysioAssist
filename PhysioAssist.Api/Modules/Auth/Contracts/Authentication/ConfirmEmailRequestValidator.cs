namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("email filed is required")
            .EmailAddress().WithMessage("please enter a valid email ");

        RuleFor(x => x.Code)
           .Length(6, 6).WithMessage("OTP must be exactly 6 digits")
           .Matches(@"^[0-9]+$").WithMessage("OTP must contain only numbers")
           .Must(BeAValidOtpRange).WithMessage("OTP must be between 100000 and 999999");
    }
    private bool BeAValidOtpRange(string? code)
    {
        if (code == null)
            return true;

        if (int.TryParse(code, out int numericCode))
        {
            // Standard 6-digit range (100000-999999)
            return numericCode >= 100000 && numericCode <= 999999;
        }
        return false;
    }
}
