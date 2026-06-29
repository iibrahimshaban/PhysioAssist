using PhysioAssist.Api.Shared.ResultPattern;

namespace PhysioAssist.Api.Modules.Auth.Errors;

public static class UserErrors
{
    public static readonly Error InvalidCredentials =
        new("User.InvalidCredentials", "Invalid email/password", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidJwtToken =
        new("User.InvalidJwtToken", "Invalid Jwt token", StatusCodes.Status401Unauthorized);

    public static readonly Error DuplicatedEmail =
        new("User.DuplicatedEmail", "This Email already Exists", StatusCodes.Status409Conflict);

    public static readonly Error EmailNotConfirmed =
    new("User.EmailNotConfirmed", "Email is not confirmed", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidCode =
        new("User.InvalidCode", "Invalid code", StatusCodes.Status401Unauthorized);

    public static readonly Error DuplicatedConfirmation =
        new("User.DuplicatedConfirmation", "Email already confirmed", StatusCodes.Status400BadRequest);

    public static readonly Error DisabledUser =
        new("User.DisabledUser", "Disabled user, Please contact the support team.", StatusCodes.Status400BadRequest);

    public static readonly Error LockedUser =
        new("User.LockedUser", "Locked user, Please Wait and try again.", StatusCodes.Status400BadRequest);

    public static readonly Error UserNotFound =
        new("User.UserNotFound", "User NotFound.", StatusCodes.Status400BadRequest);

    public static readonly Error InvalidRoles =
        new("Role.InvalidRoles", "Invalid roles", StatusCodes.Status400BadRequest);

    public static readonly Error Invalid = new(
        "User.invalid", "wrong name and password", StatusCodes.Status400BadRequest);
    public static readonly Error NotFound = new(
        "User.invalid", "wrong name and password", StatusCodes.Status404NotFound);
    public static readonly Error ExpiredToken = new(
        "User.ExpiredToken", "this token is not working any more", StatusCodes.Status400BadRequest);
    public static readonly Error InvalidRefresh = new(
        "User.InvalidRefreshToken", "can't generate a new refresh token", StatusCodes.Status400BadRequest);
}
