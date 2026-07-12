using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.DocumentationModule.Contracts;
using PhysioAssist.Api.Modules.DocumentationModule.Services;
using PhysioAssist.Api.Shared.Extensions;

namespace PhysioAssist.Api.Modules.DocumentationModule.Controller;

[ApiController]
[Route("api/documentation-summaries")]
[Authorize]
public class DocumentationSummaryController(
    IDocumentationSummaryGenerationService summaryGenerationService,
    IDocumentationSummaryPdfService pdfService) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateDocumentationSummaryRequest request)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);

        var result = await summaryGenerationService.GenerateAsync(
            doctorId, request.PatientId, request.Audience, request.Scope, request.FocusAreas);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    // Separate, on-demand — called after the doctor has reviewed the generated SummaryText.
    [HttpPost("{id:guid}/generate-pdf")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        var result = await pdfService.GeneratePdfAsync(id);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
}
