using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.InitialReportModule.DTOs;
using PhysioAssist.Api.Modules.InitialReportModule.Services;
using PhysioAssist.Api.Shared.Extensions;

namespace PhysioAssist.Api.Modules.InitialReportModule.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class InitialReportController(IInitialReportService initialReportService) : ControllerBase
{
    private readonly IInitialReportService _initialReportService = initialReportService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInitialReportRequest request)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);

        var result = await _initialReportService.CreateAsync(doctorId, request);

        if (!result.IsSuccess)
            return result.ToProblem();

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _initialReportService.GetByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:guid}/text")]
    public async Task<IActionResult> UpdateReportText(Guid id, [FromBody] UpdateReportTextRequest request)
    {
        var result = await _initialReportService.UpdateReportTextAsync(id, request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost("{id:guid}/transcribe")]
    public async Task<IActionResult> Transcribe(Guid id, IFormFile audioFile, [FromQuery] string? languageHint)
    {
        var result = await _initialReportService.TranscribeAsync(id, audioFile, languageHint);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file)
    {
        var result = await _initialReportService.UploadAttachmentAsync(id, file);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    {
        var result = await _initialReportService.DeleteAttachmentAsync(id, attachmentId);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
}
