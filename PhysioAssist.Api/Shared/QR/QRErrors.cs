namespace PhysioAssist.Api.Shared.QR;

public static class QRErrors
{
    public static readonly Error SigningKeyMissing = new(
        "QR.SigningKeyMissing",
        "QR token signing is not configured.",
        StatusCodes.Status500InternalServerError);

    public static readonly Error InvalidToken = new(
        "QR.InvalidToken",
        "The QR token is invalid.",
        StatusCodes.Status400BadRequest);

    public static readonly Error ExpiredToken = new(
        "QR.ExpiredToken",
        "The QR token has expired.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidPurpose = new(
        "QR.InvalidPurpose",
        "The QR token purpose is not valid for this operation.",
        StatusCodes.Status400BadRequest);

    public static readonly Error ImageUploadFailed = new(
    "QR.ImageUploadFailed", "Failed to upload the QR code image.", StatusCodes.Status500InternalServerError);
}
