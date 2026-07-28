namespace PhysioAssist.Api.Modules.PatientModule.Errors
{
    public static class PatientErrors
    {
        public static readonly Error NotFound =
            new("Patient.NotFound", "The requested patient was not found.", StatusCodes.Status404NotFound);

        public static readonly Error DuplicatePhone =
            new("Patient.DuplicatePhone", "A patient with this phone number already exists.", StatusCodes.Status409Conflict);

        public static readonly Error AlreadyAssigned =
    new("Patient.AlreadyAssigned", "This patient is already assigned to this doctor.", StatusCodes.Status409Conflict);

        public static readonly Error Unauthorized =
    new("Patient.Unauthorized", "You are not authenticated.", StatusCodes.Status401Unauthorized);

        public static readonly Error NotADoctor =
            new("Patient.NotADoctor", "This account is not registered as a doctor.", StatusCodes.Status403Forbidden);

        public static readonly Error DuplicateEmail =
    new("Patient.DuplicateEmail", "A patient with this email address already exists.", StatusCodes.Status409Conflict);

        public static readonly Error InvalidIntakeSubmission =
    new("Patient.InvalidIntakeSubmission", "The intake form submission could not be processed.", StatusCodes.Status400BadRequest);
    }
}