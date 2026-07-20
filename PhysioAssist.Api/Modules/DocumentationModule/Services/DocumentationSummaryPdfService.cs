using PhysioAssist.Api.Modules.DocumentationModule.Contracts;
using PhysioAssist.Api.Modules.DocumentationModule.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Pdf;
using System.Text.Json;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public class DocumentationSummaryPdfService(
    ApplicationDbContext context,
    IPdfService pdfService) : IDocumentationSummaryPdfService
{
    private const string cloudinaryFolder = "documentation-summaries";

    public async Task<Result<DocumentationSummaryResponse>> GeneratePdfAsync(Guid documentationSummaryId, CancellationToken ct = default)
    {
        var summary = await context.DocumentationSummaries
            .FirstOrDefaultAsync(s => s.Id == documentationSummaryId && !s.IsDeleted, ct);

        if (summary is null)
            return Result.Failure<DocumentationSummaryResponse>(DocumentationErrors.DocumentationSummaryNotFound);

        var focusAreas = string.IsNullOrWhiteSpace(summary.FocusAreas)
            ? null
            : JsonSerializer.Deserialize<List<string>>(summary.FocusAreas);

        var sections = new List<PdfSection>
            {
                new PdfSection(null,
                [
                    $"Audience: {summary.Audience}",
                    $"Scope: {summary.Scope}",
                    $"Date: {summary.CreatedAt:yyyy-MM-dd}"
                ]),
                new PdfSection("Summary", [summary.SummaryText])
            };

        if (focusAreas is { Count: > 0 })
            sections.Insert(1, new PdfSection("Focus Areas", [string.Join(", ", focusAreas)]));

        var content = new PdfDocumentContent(
            Title: "PhysioAssist — Documentation Summary",
            Sections: sections);

        var pdfResult = await pdfService.GeneratePdfAsync(
            content, cloudinaryFolder, summary.Id.ToString(), ct);

        if (pdfResult.IsFailure)
            return Result.Failure<DocumentationSummaryResponse>(pdfResult.Error);

        summary.FileUrl = pdfResult.Value;
        await context.SaveChangesAsync(ct);

        return Result.Success(new DocumentationSummaryResponse(
            summary.Id, summary.Audience, summary.Scope, focusAreas,
            summary.AnonymizePersonalData, summary.SummaryText, summary.FileUrl, summary.CreatedAt));
    }
}