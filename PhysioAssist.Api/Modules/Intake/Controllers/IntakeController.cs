using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;
using PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;
using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;
using PhysioAssist.Api.Modules.Intake.Services;
using PhysioAssist.Api.Shared.Authorization;
using PhysioAssist.Api.Shared.Consts;
using PhysioAssist.Api.Shared.Extensions;

namespace PhysioAssist.Api.Modules.Intake.Controllers;

[Route("api/intake")]
[ApiController]
public class IntakeController(IIntakeService intakeService) : ControllerBase
{
    private readonly IIntakeService _intakeService = intakeService;

    [HttpPost("form-schemas")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> CreateFormSchema([FromBody] CreateFormSchemaRequest request, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.CreateFormSchemaAsync(request, doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("form-schemas/{schemaId:guid}")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> UpdateFormSchema(Guid schemaId, [FromBody] UpdateFormSchemaRequest request, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.UpdateFormSchemaAsync(schemaId, request, doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("form-schemas/{schemaId:guid}/publish")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> PublishFormSchema(Guid schemaId, [FromBody] PublishFormSchemaRequest request, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.PublishFormSchemaAsync(schemaId, request, doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("form-schemas/{schemaId:guid}")]
    [HasPermission(Permissions.IntakeRead)]
    public async Task<IActionResult> GetFormSchemaById(Guid schemaId, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.GetFormSchemaByIdAsync(schemaId, doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("form-schemas")]
    [HasPermission(Permissions.IntakeRead)]
    public async Task<IActionResult> GetFormSchemasByDoctor(CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.GetFormSchemasByDoctorAsync(doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("form-schemas/default")]
    [HasPermission(Permissions.IntakeRead)]
    public async Task<IActionResult> GetDefaultFormSchema(CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.GetDefaultFormSchemaAsync(doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("form-schemas/{id:guid}/qr-link")]
    [HasPermission(Permissions.QRGenerate)]
    public async Task<IActionResult> GenerateIntakeQrLink(Guid id, [FromBody] GenerateIntakeQrLinkRequest request, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.GenerateIntakeQrLinkAsync(id, request, doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("submissions")]
    [HasPermission(Permissions.IntakeReview)]
    public async Task<IActionResult> GetSubmissions([FromQuery] IntakeStatus? status, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.GetSubmissionsAsync(doctorId, status, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("submissions/{id:guid}")]
    [HasPermission(Permissions.IntakeReview)]
    public async Task<IActionResult> GetSubmissionDetails(Guid id, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.GetSubmissionDetailsAsync(id, doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("submissions/{id:guid}/status")]
    [HasPermission(Permissions.IntakeReview)]
    public async Task<IActionResult> UpdateIntakeStatus(Guid id, [FromBody] UpdateIntakeStatusRequest request, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.UpdateStatusAsync(id, request, doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
