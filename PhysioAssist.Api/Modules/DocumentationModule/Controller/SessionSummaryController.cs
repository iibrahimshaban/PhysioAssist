using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.DocumentationModule.Services;

namespace PhysioAssist.Api.Modules.DocumentationModule.Controller;

[ApiController]
[Route("api/sessions/{sessionId:guid}/summary")]
[Authorize]
public class SessionSummaryController(ISessionSummaryGenerationService summaryGenerationService) : ControllerBase
{
    // Should be called after the progress note's Subjective/Assessment/Plan are finalized —
    // reads the note, generates a narrative summary via Groq, writes it onto Session.
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(Guid sessionId)
    {
        var result = await summaryGenerationService.GenerateAndSaveSummaryAsync(sessionId);
        return result.IsFailure ? result.ToProblem() : Ok();
    }
}
