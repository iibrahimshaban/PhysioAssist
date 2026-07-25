using PhysioAssist.Api.Shared.Dtos.Pdf;

namespace PhysioAssist.Api.Shared.Interfaces.Common;

public interface IPdfService
{
    /// <summary>
    /// Generates a treatment plan PDF from the given content and uploads it,
    /// returning the public URL of the stored file.
    /// </summary>
    Task<Result<string>> GeneratePdfAsync(PdfDocumentContent content, string folder, string publicId, CancellationToken ct = default);
}
