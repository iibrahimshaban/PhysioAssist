namespace PhysioAssist.Api.Modules.InitialReportModule.Errors;

public static class InitialReportErrors
{
    public static readonly Error NotFound =
        new("InitialReport.NotFound", "Initial report not found.", StatusCodes.Status404NotFound);

    public static readonly Error AttachmentNotFound =
        new("InitialReport.AttachmentNotFound", "Attachment not found.", StatusCodes.Status404NotFound);

    public static readonly Error TranscriptionFailed =
        new("InitialReport.TranscriptionFailed", "Voice transcription failed.", StatusCodes.Status422UnprocessableEntity);

    public static readonly Error AttachmentUploadFailed =
        new("InitialReport.AttachmentUploadFailed", "Attachment upload failed.", StatusCodes.Status422UnprocessableEntity);

    public static readonly Error InvalidFileType =
        new("InitialReport.InvalidFileType", "Only image files are supported for attachments.", StatusCodes.Status400BadRequest);
}
