using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.InitialReportModule.DTOs;
using PhysioAssist.Api.Modules.InitialReportModule.Services;


namespace PhysioAssist.Api.Modules.InitialReportModule.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class InitialReportController(
    IInitialReportService _initialReportService,
    IIntakeQueryService _intakeQueryService,
    ITreatmentSchedulePlanService _treatmentSchedulePlanService) : ControllerBase
{
    [HttpPost]
    [HasPermission(Permissions.WriteInitialReport)]
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
    [HasPermission(Permissions.ReadInitialReport)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _initialReportService.GetByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:guid}/text")]
    [HasPermission(Permissions.WriteInitialReport)]
    public async Task<IActionResult> UpdateReportText(Guid id, [FromBody] UpdateReportTextRequest request)
    {
        var result = await _initialReportService.UpdateReportTextAsync(id, request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost("{id:guid}/transcribe")]
    [HasPermission(Permissions.WriteInitialReport)]
    public async Task<IActionResult> Transcribe(Guid id, IFormFile audioFile, [FromQuery] string? languageHint)
    {
        var result = await _initialReportService.TranscribeAsync(id, audioFile, languageHint);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost("{id:guid}/attachments")]
    [HasPermission(Permissions.WriteInitialReport)]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file)
    {
        var result = await _initialReportService.UploadAttachmentAsync(id, file);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    [HasPermission(Permissions.WriteInitialReport)]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    {
        var result = await _initialReportService.DeleteAttachmentAsync(id, attachmentId);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    [HttpGet("patient/{patientId:guid}/intake")]
    [HasPermission(Permissions.ReadInitialReport)]
    public async Task<IActionResult> GetIntakeDataByPatientId(Guid patientId)
    {
        var result = await _intakeQueryService.GetPreVisitIntakeByPatientIdAsync(patientId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpGet("patient/{patientId:guid}")]
    [HasPermission(Permissions.ReadInitialReport)]
    public async Task<IActionResult> GetByPatientId(Guid patientId)
    {
        var result = await _initialReportService.GetByPatientIdAsync(patientId);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpGet("patient/{patientId:guid}/summary")]
    [HasPermission(Permissions.ReadInitialReport)]
    public async Task<IActionResult> GetIntakeDataSummaryByPatientId(Guid patientId)
    {
        var result = await _intakeQueryService.GetPatientIntakeSummaryAsync(patientId);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost("{id:guid}/submit")]
    [HasPermission(Permissions.WriteInitialReport)]
    public async Task<IActionResult> Submit(Guid id)
    {
        var result = await _initialReportService.SubmitAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost("{id:guid}/schedule-plan")]
    [HasPermission(Permissions.WriteInitialReport)]
    public async Task<IActionResult> UpsertSchedulePlan(Guid id, [FromBody] UpsertTreatmentSchedulePlanRequest request)
    {
        var result = await _treatmentSchedulePlanService.UpsertAsync(id, request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpGet("{id:guid}/schedule-plan")]
    [HasPermission(Permissions.ReadInitialReport)]
    public async Task<IActionResult> GetSchedulePlan(Guid id)
    {
        var result = await _treatmentSchedulePlanService.GetAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost("{id:guid}/schedule-plan/book")]
    [HasPermission(Permissions.WriteInitialReport)]
    public async Task<IActionResult> BookSchedulePlan(Guid id, [FromBody] BookTreatmentSlotRequest request)
    {
        var result = await _treatmentSchedulePlanService.BookNowAsync(id, request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost("{id:guid}/schedule-plan/send-to-receptionist")]
    [HasPermission(Permissions.WriteInitialReport)]
    public async Task<IActionResult> SendSchedulePlanToReceptionist(Guid id)
    {
        var result = await _treatmentSchedulePlanService.SendToReceptionistAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}