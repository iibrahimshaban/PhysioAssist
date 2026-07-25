namespace PhysioAssist.Api.Modules.Auth.Errors;

public static class ReceptionistErrors
{
    public static readonly Error ReceptionistMustHaveOwner =
        new("User.ReceptionistMustHaveOwner", "Receptionists must have an associated doctor.", StatusCodes.Status400BadRequest);

    public static readonly Error EmailTaken =
        new("Receptionist.EmailTaken", "Email already in use.", StatusCodes.Status409Conflict);

    public static Error CreateFailed(string details) =>
        new("Receptionist.CreateFailed", details, StatusCodes.Status400BadRequest);

    public static readonly Error NotFound =
        new("Receptionist.NotFound", "Receptionist not found.", StatusCodes.Status404NotFound);

    public static readonly Error DoctorNotResolved =
        new("Receptionist.DoctorNotResolved", "Could not resolve the managing doctor for this account.", StatusCodes.Status403Forbidden);

    public static readonly Error PackageNotFound =
        new("Receptionist.PackageNotFound", "Session package not found.", StatusCodes.Status404NotFound);

    public static readonly Error PackageAccessDenied =
        new("Receptionist.PackageAccessDenied", "You do not have access to this package.", StatusCodes.Status403Forbidden);
}
