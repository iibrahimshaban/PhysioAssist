using PhysioAssist.Api.Shared.Consts;

namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public class RegistrationRequestValidator : AbstractValidator<RegistrationRequest>
{
    public RegistrationRequestValidator()
    {

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

        RuleFor(x => x.ProfilePhoto)
            .Must(BeAValidImage)
            .WithMessage("Only JPG , JPEG and PNG images are allowed.")
            .When(f => f.ProfilePhoto != null)
            .Must(f => f == null || f.Length <= 2 * 1024 * 1024)
            .WithMessage("Image size must be 2MB or less.")
            .When(f => f.ProfilePhoto != null);
    }

    private bool BeAValidImage(IFormFile? file)
    {
        if (file == null) return true; // Allow null

        var allowedContentTypes = new[] { "image/jpeg", "image/png" };
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

        var contentTypeIsValid = allowedContentTypes.Contains(file.ContentType);
        var extensionIsValid = allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());

        return contentTypeIsValid && extensionIsValid;
    }
}
