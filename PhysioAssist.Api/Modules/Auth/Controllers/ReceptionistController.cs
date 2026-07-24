using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Contracts.Receptionist;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Modules.Auth.Services;
using PhysioAssist.Api.Modules.InitialReportModule.Errors;
using PhysioAssist.Api.Modules.InitialReportModule.Services;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.Auth.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReceptionistController(
    IReceptionistService _receptionistService,
    IPatientSessionSchedulingService _schedulingService,
    ITreatmentSchedulePlanService _treatmentSchedulePlanService,
    ApplicationDbContext _dbContext) : ControllerBase
{

    [HttpGet]
    [HasPermission(Permissions.GetReceptionist)]
    public async Task<IActionResult> GetAll()
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _receptionistService.GetAllAsync(doctorId);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.GetReceptionist)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _receptionistService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("permissions")]
    public IActionResult GetAssignablePermissions()
    {
        var doctorPermissions = User.Claims
            .Where(c => c.Type == Permissions.Type)
            .Select(c => c.Value)
            .Distinct()
            .Where(Permissions.Metadata.ContainsKey)
            .Select(v => Permissions.Metadata[v]);

        return Ok(doctorPermissions);
    }

    [HttpPost]
    [HasPermission(Permissions.CreateReceptionist)]
    public async Task<IActionResult> Create([FromBody] CreateReceptionistRequest request, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _receptionistService.CreateAsync(doctorId, request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.UpdateReceptionist)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReceptionistRequest request, CancellationToken cancellationToken)
    {
        var result = await _receptionistService.UpdateAsync(id, request,cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{id:guid}/toggle-disabled")]
    [HasPermission(Permissions.UpdateReceptionist)]
    public async Task<IActionResult> ToggleDisabled(Guid id)
    {
        var result = await _receptionistService.ToggleDisabledAsync(id);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.UpdateReceptionist)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _receptionistService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
    [HttpPost("packages")]
    [HasPermission(Permissions.CreateSessionPackage)]
    public async Task<IActionResult> CreatePackage(
        [FromBody] ReceptionistCreateSessionPackageRequest request,
        CancellationToken cancellationToken)
    {
        var managingDoctorId = await User.GetDoctorIdAsync(_dbContext, cancellationToken);
        if (managingDoctorId is null)
            return Result.Failure(ReceptionistErrors.DoctorNotResolved).ToProblem();

        // DoctorId is never taken from the client — always the resolved managing
        // doctor, so a receptionist can only ever create packages for the doctor
        // they actually work for.
        var fullRequest = new CreateSessionPackageRequest
        {
            PatientId = request.PatientId,
            DoctorId = managingDoctorId.Value,
            TotalSessions = request.TotalSessions,
            SessionDuration = request.SessionDuration,
            SessionsPerWeek = request.SessionsPerWeek,
            MinimumGapBetweenSessionsDays = request.MinimumGapBetweenSessionsDays,
            PreferredTimeOfDay = request.PreferredTimeOfDay,
            PreferredDays = request.PreferredDays,
            Priority = request.Priority,
            FirstSessionSlot = request.FirstSessionSlot
        };

        var result = await _schedulingService.CreatePackageAsync(fullRequest, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("packages/{packageId:guid}/next-candidates")]
    [HasPermission(Permissions.GetSessionCandidates)]
    public async Task<IActionResult> GetNextSessionCandidates(
    Guid packageId,
    [FromBody] GetNextSessionCandidatesRequest? request,
    CancellationToken cancellationToken)
    {
        var ownershipCheck = await EnsurePackageBelongsToCallerAsync(packageId, cancellationToken);
        if (ownershipCheck is not null)
            return ownershipCheck;

        var result = await _schedulingService.GetNextSessionCandidatesAsync(
            packageId,
            request?.PatientFreeTimeOverride,
            request?.PersistFreeTimeOverride ?? false,
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("packages/{packageId:guid}/confirm-slot")]
    [HasPermission(Permissions.ConfirmSessionSlot)]
    public async Task<IActionResult> ConfirmSessionSlot(
        Guid packageId,
        [FromBody] SlotCandidateDto chosenSlot,
        CancellationToken cancellationToken)
    {
        var ownershipCheck = await EnsurePackageBelongsToCallerAsync(packageId, cancellationToken);
        if (ownershipCheck is not null)
            return ownershipCheck;

        var result = await _schedulingService.ConfirmSessionSlotAsync(packageId, chosenSlot, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    
    [HttpGet("packages/{packageId:guid}/summary")]
    [HasPermission(Permissions.GetSessionCandidates)]
    public async Task<IActionResult> GetPackageSummary(Guid packageId, CancellationToken cancellationToken)
    {
        var ownershipCheck = await EnsurePackageBelongsToCallerAsync(packageId, cancellationToken);
        if (ownershipCheck is not null)
            return ownershipCheck;

        var result = await _schedulingService.GetPackageSummaryAsync(packageId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
    [HttpGet("patients/{patientId:guid}/scheduling-context")]
    [HasPermission(Permissions.GetSessionCandidates)]
    public async Task<IActionResult> GetSchedulingContext(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await _treatmentSchedulePlanService.GetSchedulingContextForPatientAsync(patientId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("treatment-plans/{treatmentPlanId:guid}/convert-to-package")]
    [HasPermission(Permissions.CreateSessionPackage)]
    public async Task<IActionResult> ConvertPlanToPackage(
        Guid treatmentPlanId, [FromBody] ConvertPlanToPackageRequest request, CancellationToken cancellationToken)
    {

        var ownershipCheck = await EnsureTreatmentPlanBelongsToCallerAsync(treatmentPlanId, cancellationToken);

        if (ownershipCheck is not null)
            return ownershipCheck;

        var result = await _treatmentSchedulePlanService.ConvertPlanToPackageAsync(treatmentPlanId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
    private async Task<IActionResult?> EnsureTreatmentPlanBelongsToCallerAsync(Guid treatmentPlanId, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(_dbContext, cancellationToken);
        if (doctorId is null)
            return Result.Failure(ReceptionistErrors.DoctorNotResolved).ToProblem();

        var planDoctorId = await _treatmentSchedulePlanService.GetPlanDoctorIdAsync(treatmentPlanId, cancellationToken);
        if (planDoctorId is null)
            return Result.Failure(TreatmentSchedulePlanErrors.NotFound).ToProblem();

        if (planDoctorId.Value != doctorId.Value)
            return Result.Failure(TreatmentSchedulePlanErrors.AccessDenied).ToProblem();

        return null;
    }
    private async Task<IActionResult?> EnsurePackageBelongsToCallerAsync(Guid packageId, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(_dbContext, cancellationToken);
        if (doctorId is null)
            return Result.Failure(ReceptionistErrors.DoctorNotResolved).ToProblem();

        var packageDoctorId = await _schedulingService.GetPackageDoctorIdAsync(packageId, cancellationToken);
        if (packageDoctorId is null)
            return Result.Failure(ReceptionistErrors.PackageNotFound).ToProblem();

        if (packageDoctorId.Value != doctorId.Value)
            return Result.Failure(ReceptionistErrors.PackageAccessDenied).ToProblem();

        return null;
    }
}
