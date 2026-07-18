namespace PhysioAssist.Api.Modules.Auth.Errors;

public static class UserErrors
{
    public static readonly Error InvalidCredentials =
        new("User.InvalidCredentials", "The email or password you entered is incorrect.", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidJwtToken =
        new("User.InvalidJwtToken", "The provided token is invalid or has been tampered with.", StatusCodes.Status401Unauthorized);

    public static readonly Error ExpiredToken =
        new("User.ExpiredToken", "Your session has expired. Please log in again.", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidRefresh =
        new("User.InvalidRefreshToken", "The refresh token is invalid, expired, or has already been used.", StatusCodes.Status401Unauthorized);

    public static readonly Error EmailNotConfirmed =
        new("User.EmailNotConfirmed", "Please confirm your email address before logging in.", StatusCodes.Status401Unauthorized);

    public static readonly Error DuplicatedEmail =
        new("User.DuplicatedEmail", "An account with this email address already exists.", StatusCodes.Status409Conflict);
    public static readonly Error DoublicatedUserName =
        new("User.DoublicatedUserName", "An account with this username already exists.", StatusCodes.Status409Conflict);

    public static readonly Error DuplicatedConfirmation =
        new("User.DuplicatedConfirmation", "This email address has already been confirmed.", StatusCodes.Status409Conflict);

    public static readonly Error InvalidCode =
        new("User.InvalidCode", "The verification code is invalid or has expired.", StatusCodes.Status400BadRequest);

    public static readonly Error DisabledUser =
        new("User.DisabledUser", "Your account has been disabled. Please contact support for assistance.", StatusCodes.Status403Forbidden);

    public static readonly Error LockedUser =
        new("User.LockedUser", "Your account has been temporarily locked due to multiple failed attempts. Please try again later.", StatusCodes.Status403Forbidden);

    public static readonly Error UserNotFound =
        new("User.UserNotFound", "No account was found with the provided information.", StatusCodes.Status404NotFound);

    public static readonly Error RoleNotFound =
        new("User.RoleNotFound", "One or more of the specified roles do not exist.", StatusCodes.Status404NotFound);

    public static readonly Error InvalidRoles =
        new("User.InvalidRoles", "One or more of the specified roles do not exist.", StatusCodes.Status400BadRequest);

    public static readonly Error ResetPasswordFailed =
        new("User.ResetPasswordFailed", "Password reset failed. Please request a new reset code and try again.", StatusCodes.Status400BadRequest);

    public static readonly Error RegistrationFailed =
        new("User.RegistrationFailed", "Registration could not be completed. Please try again later.", StatusCodes.Status400BadRequest);

    public static readonly Error ReceptionistMustHaveOwner =
        new("User.ReceptionistMustHaveOwner", "Receptionists must have an associated doctor.", StatusCodes.Status400BadRequest);
}