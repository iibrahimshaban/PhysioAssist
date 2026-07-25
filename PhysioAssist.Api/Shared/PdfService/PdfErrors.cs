namespace PhysioAssist.Api.Shared.PdfService;

public static class PdfErrors
{
    public static readonly Error GenerationFailed = new(
        "Pdf.GenerationFailed",
        "Failed to generate the PDF document.",
        StatusCodes.Status500InternalServerError);

    public static readonly Error UploadFailed = new(
        "Pdf.UploadFailed",
        "Failed to upload the generated PDF.",
        StatusCodes.Status500InternalServerError);
}
