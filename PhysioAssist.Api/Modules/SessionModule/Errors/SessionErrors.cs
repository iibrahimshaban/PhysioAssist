namespace PhysioAssist.Api.Modules.SessionModule.Errors
{
    public static class SessionErrors
    {
        public static readonly Error SessionNotFound =
        new(
            "Session.NotFound",
            "Session was not found.",
            StatusCodes.Status404NotFound
        );
        public static readonly Error InvalidSessionStatus =
    new("Session.InvalidStatus", "Session cannot be started.", StatusCodes.Status400BadRequest);
    }
}
