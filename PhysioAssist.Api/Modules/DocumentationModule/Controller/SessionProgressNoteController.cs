using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.DocumentationModule.Contracts;
using PhysioAssist.Api.Modules.DocumentationModule.Services;
using PhysioAssist.Api.Shared.Interfaces.Documentation;

namespace PhysioAssist.Api.Modules.DocumentationModule.Controller;

[ApiController]
[Route("api/sessions/{sessionId:guid}/progress-note")]
[Authorize]
public class SessionProgressNoteController(
    ISessionProgressNoteExtractionService extractionService,
    ISessionProgressNoteService progressNoteService) : ControllerBase
{
    // Triggers GPT-4o-mini extraction of Objective findings from the session transcript.
    // Safe to call again later — it upserts rather than duplicating the note.
    [HttpPost("generate-objective-findings")]
    public async Task<IActionResult> GenerateObjectiveFindings(Guid sessionId)
    {
        var result = await extractionService.GenerateObjectiveFindingsAsync(sessionId);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid sessionId)
    {
        var result = await progressNoteService.GetBySessionIdAsync(sessionId);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    // Doctor-authored Subjective/Assessment/Plan text — never touches ObjectiveFindings.
    [HttpPut]
    public async Task<IActionResult> UpdateNarrative(Guid sessionId, [FromBody] UpdateProgressNoteNarrativeRequest request)
    {
        var result = await progressNoteService.UpdateNarrativeAsync(
            sessionId, request.Subjective, request.Assessment, request.Plan);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
}
