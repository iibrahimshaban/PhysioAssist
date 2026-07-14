using PhysioAssist.Api.Modules.DocumentationModule.Contracts;
using PhysioAssist.Api.Modules.DocumentationModule.Errors;
using PhysioAssist.Api.Modules.DocumentationModule.Helpers;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces.Common;
using System.Text.Json;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public class DocumentationSummaryPdfService(
    ApplicationDbContext context,
    IMediaStorageService mediaStorageService) : IDocumentationSummaryPdfService
{
    private const string CloudinaryFolder = "documentation-summaries";

    public async Task<Result<DocumentationSummaryResponse>> GeneratePdfAsync(Guid documentationSummaryId, CancellationToken ct = default)
    {
        var summary = await context.DocumentationSummaries
            .FirstOrDefaultAsync(s => s.Id == documentationSummaryId && !s.IsDeleted, ct);

        if (summary is null)
            return Result.Failure<DocumentationSummaryResponse>(DocumentationErrors.DocumentationSummaryNotFound);

        var pdfBytes = DocumentationSummaryPdfRenderer.Render(summary);
        var fileName = $"documentation-summary-{summary.Id}.pdf";
        var publicId = summary.Id.ToString();

        // IMediaStorageService.UploadImageAsync takes an IFormFile — wrap the generated
        // PDF bytes in one, same as how TreatmentPlanPdfUrl presumably already does this
        // for non-image documents.
        await using var stream = new MemoryStream(pdfBytes);
        var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        // IMediaStorageService.UploadDocumentAsync (raw upload, no transformation) —
        // NOT UploadImageAsync, which applies a fixed 500x500 crop meant for profile pictures.
        var fileUrl = await mediaStorageService.UploadDocumentAsync(formFile, CloudinaryFolder, publicId);

        summary.FileUrl = fileUrl;
        await context.SaveChangesAsync(ct);

        var focusAreas = string.IsNullOrWhiteSpace(summary.FocusAreas)
            ? null
            : JsonSerializer.Deserialize<List<string>>(summary.FocusAreas);

        return Result.Success(new DocumentationSummaryResponse(
            summary.Id, summary.Audience, summary.Scope, focusAreas,
            summary.AnonymizePersonalData, summary.SummaryText, summary.FileUrl, summary.CreatedAt));
    }
}
