namespace PhysioAssist.Api.Modules.Auth.Errors;

public static class DoctorErrors
{
    public static readonly Error DoctorNotFound =
        new("Doctor.DoctorNotFound", "No doctor was found with the provided information.", StatusCodes.Status404NotFound);

}
