using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;
using PhysioAssist.Api.Modules.Intake.DTOs.PublicAccess;
using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;
using PhysioAssist.Api.Modules.Intake.Services;

namespace PhysioAssist.Api.Modules.Intake.Controllers;

[Route("api/intake")]
[ApiController]
[Authorize]
public class IntakeController(IIntakeService intakeService, ApplicationDbContext context) : ControllerBase
{
    private readonly IIntakeService _intakeService = intakeService;

    [HttpPost("form-schemas")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> CreateFormSchema([FromBody] CreateFormSchemaRequest request, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.CreateFormSchemaAsync(request, doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("form-schemas/{schemaId:guid}")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> UpdateFormSchema(Guid schemaId, [FromBody] UpdateFormSchemaRequest request, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.UpdateFormSchemaAsync(schemaId, request, doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("form-schemas/{schemaId:guid}/publish")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> PublishFormSchema(Guid schemaId, [FromBody] PublishFormSchemaRequest request, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.PublishFormSchemaAsync(schemaId, request, doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("form-schemas/{schemaId:guid}")]
    [HasPermission(Permissions.IntakeRead)]
    public async Task<IActionResult> GetFormSchemaById(Guid schemaId, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.GetFormSchemaByIdAsync(schemaId, doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("form-schemas")]
    [HasPermission(Permissions.IntakeRead)]
    public async Task<IActionResult> GetFormSchemasByDoctor(CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.GetFormSchemasByDoctorAsync(doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("form-schemas/default")]
    [HasPermission(Permissions.IntakeRead)]
    public async Task<IActionResult> GetDefaultFormSchema(CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.GetDefaultFormSchemaAsync(doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("form-schemas/{id:guid}/qr-link")]
    [HasPermission(Permissions.QRGenerate)]
    public async Task<IActionResult> GenerateIntakeQrLink(Guid id, [FromBody] GenerateIntakeQrLinkRequest request, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.GenerateIntakeQrLinkAsync(id, request, doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("submissions")]
    [HasPermission(Permissions.SubmissionRead)]
    public async Task<IActionResult> GetSubmissions([FromQuery] IntakeStatus? status, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.GetSubmissionsAsync(doctorId!.Value, status, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("submissions/{id:guid}")]
    [HasPermission(Permissions.SubmissionRead)]
    public async Task<IActionResult> GetSubmissionDetails(Guid id, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.GetSubmissionDetailsAsync(id, doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("submissions/{id:guid}/status")]
    [HasPermission(Permissions.IntakeConvert)]
    public async Task<IActionResult> UpdateIntakeStatus(Guid id, [FromBody] UpdateIntakeStatusRequest request, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.UpdateStatusAsync(id, request, doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("form-schemas/default")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> GenerateDefaultFormSchema(CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.GenerateDefaultFormSchemaAsync(doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("form-schemas/{schemaId:guid}/duplicate")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> DuplicateFormSchema(Guid schemaId, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.DuplicateFormSchemaAsync(schemaId, doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("form-schemas/{schemaId:guid}")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> DeleteFormSchema(Guid schemaId, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.DeleteFormSchemaAsync(schemaId, doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    [HttpPost("form-schemas/{schemaId:guid}/archive")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> ArchiveFormSchema(Guid schemaId, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.ArchiveFormSchemaAsync(schemaId, doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    [HttpPost("form-schemas/{schemaId:guid}/unarchive")]
    [HasPermission(Permissions.IntakeManageForms)]
    public async Task<IActionResult> UnarchiveFormSchema(Guid schemaId, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(context, cancellationToken);
        var result = await _intakeService.UnarchiveFormSchemaAsync(schemaId, doctorId!.Value, cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    [HttpPost("submissions/{id:guid}/convert-to-patient")]
    [HasPermission(Permissions.IntakeConvert)]
    public async Task<IActionResult> ConvertToPatient(Guid id, [FromBody] ConvertIntakeToPatientRequest request, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _intakeService.ConvertToPatientAsync(id, request, doctorId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
