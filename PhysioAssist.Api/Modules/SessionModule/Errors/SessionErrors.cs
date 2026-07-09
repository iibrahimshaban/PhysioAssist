namespace PhysioAssist.Api.Modules.SessionModule.Errors
{
    public static class SessionErrors
    {
        public static readonly Error SessionNotFound =
            new(
                "Session.NotFound",
                "The requested session was not found.",
                StatusCodes.Status404NotFound);

        public static readonly Error InvalidSessionStatus =
            new(
                "Session.InvalidStatus",
                "The session cannot be started in its current state.",
                StatusCodes.Status400BadRequest);

        public static readonly Error EmptyAudioFile =
            new(
                "Session.EmptyAudioFile",
                "No audio file was uploaded.",
                StatusCodes.Status400BadRequest);

        public static readonly Error EmptyAttachmentFile =
            new(
                "Session.EmptyAttachmentFile",
                "No attachment files were uploaded.",
                StatusCodes.Status400BadRequest);

        public static readonly Error AttachmentNotFound =
            new(
                "Session.AttachmentNotFound",
                "The requested attachment was not found.",
                StatusCodes.Status404NotFound);

        public static readonly Error TranscriptionNotFound =
            new(
                "Session.TranscriptionNotFound",
                "No transcription was found for this session.",
                StatusCodes.Status404NotFound);

        public static readonly Error SessionAlreadyCompleted =
            new(
                "Session.AlreadyCompleted",
                "This session has already been completed.",
                StatusCodes.Status409Conflict);

        public static readonly Error InvalidTranscript =
            new(
                "Session.InvalidTranscript",
                "The session transcript is required.",
                StatusCodes.Status400BadRequest);
    }
}
