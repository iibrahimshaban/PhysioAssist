using PhysioAssist.Api.Modules.DocumentationModule.Contracts;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public interface IDocumentationSummaryPdfService
{
    Task<Result<DocumentationSummaryResponse>> GeneratePdfAsync(Guid documentationSummaryId, CancellationToken ct = default);
}
