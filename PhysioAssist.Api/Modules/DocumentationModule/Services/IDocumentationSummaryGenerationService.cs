using PhysioAssist.Api.Modules.DocumentationModule.Contracts;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public interface IDocumentationSummaryGenerationService
{
    Task<Result<DocumentationSummaryResponse>> GenerateAsync(
        Guid doctorId,
        Guid patientId,
        SummaryAudience audience,
        SummaryScope? scope,
        List<string>? focusAreas,
        CancellationToken ct = default);
}
