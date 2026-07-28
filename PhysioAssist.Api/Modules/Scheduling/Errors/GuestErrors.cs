namespace PhysioAssist.Api.Modules.Scheduling.Errors
{
    public static class GuestErrors
    {
        public static Error FullNameRequired => new(
            "Guest.FullNameRequired",
            "Guest full name is required.",
            StatusCodes.Status400BadRequest);

        public static Error PhoneNumberRequired => new(
            "Guest.PhoneNumberRequired",
            "Guest phone number is required.",
            StatusCodes.Status400BadRequest);

        public static Error NotFound(Guid guestId) => new(
            "Guest.NotFound",
            $"Guest {guestId} was not found.",
            StatusCodes.Status404NotFound);
    }
}
