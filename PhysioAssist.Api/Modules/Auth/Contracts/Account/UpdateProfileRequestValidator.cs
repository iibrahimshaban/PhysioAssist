using PhysioAssist.Api.Shared.Consts;

namespace PhysioAssist.Api.Modules.Auth.Contracts.Account;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.UserName)
           .NotEmpty().WithMessage("Username is required.")
           .Matches(RegexPattern.UserName).WithMessage("Username can only have letters and numbers");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.");

        RuleFor(x => x.PhoneNumber)
            .Matches(RegexPattern.PhoneNumber).WithMessage("Please enter a valid phone number.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Title)
            .MaximumLength(150).WithMessage("Title must be 150 characters or less.")
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.ClinicName)
            .MaximumLength(200).WithMessage("Clinic name must be 200 characters or less.")
            .When(x => !string.IsNullOrWhiteSpace(x.ClinicName));

        RuleFor(x => x.ClinicAddress)
            .MaximumLength(300).WithMessage("Clinic address must be 300 characters or less.")
            .When(x => !string.IsNullOrWhiteSpace(x.ClinicAddress));

        RuleFor(x => x.About)
            .MaximumLength(1000).WithMessage("About section must be 1000 characters or less.")
            .When(x => !string.IsNullOrWhiteSpace(x.About));

        RuleFor(x => x.YearsOfExperience)
            .InclusiveBetween(0, 70).WithMessage("Years of experience must be between 0 and 70.")
            .When(x => x.YearsOfExperience.HasValue);

        RuleFor(x => x.ProfilePhoto)
            .Must(BeAValidImage)
            .WithMessage("Only JPG, JPEG and PNG images are allowed.")
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