using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Shared.Dtos.Package;

namespace PhysioAssist.Api.Modules.PackageModule.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PackageController(
    IPatientSessionPackageService _packageService,
    ISessionAssignmentService _sessionAssignmentService,
    ApplicationDbContext _dbContext) : ControllerBase
{
    [HttpGet("{packageId:guid}/summary")]
    [HasPermission(Permissions.ManageSchedule)]
    public async Task<IActionResult> GetSummary(Guid packageId, CancellationToken cancellationToken)
    {
        var ownershipCheck = await EnsurePackageBelongsToCallerAsync(packageId, cancellationToken);
        if (ownershipCheck is not null) return ownershipCheck;

        var result = await _packageService.GetPackageSummaryAsync(packageId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("{packageId:guid}/extend")]
    [HasPermission(Permissions.ManageSchedule)]
    public async Task<IActionResult> Extend(
        Guid packageId, [FromBody] ExtendPackageRequest request, CancellationToken cancellationToken)
    {
        var ownershipCheck = await EnsurePackageBelongsToCallerAsync(packageId, cancellationToken);
        if (ownershipCheck is not null) return ownershipCheck;

        var actingUserId = Guid.Parse(User.GetUserId()!);
        var result = await _packageService.ExtendPackageAsync(packageId, request, actingUserId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("{packageId:guid}/stop")]
    [HasPermission(Permissions.ManageSchedule)]
    public async Task<IActionResult> Stop(
        Guid packageId, [FromBody] StopPackageRequest request, CancellationToken cancellationToken)
    {
        var ownershipCheck = await EnsurePackageBelongsToCallerAsync(packageId, cancellationToken);
        if (ownershipCheck is not null) return ownershipCheck;

        var actingUserId = Guid.Parse(User.GetUserId()!);
        var result = await _packageService.StopPackageAsync(packageId, request, actingUserId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Default substitution — any doctor/JuniorDoctor can substitute for their OWN
    /// package's single session. No elevated permission required beyond ownership.
    /// </summary>
    [HttpPost("{packageId:guid}/substitute-doctor")]
    [HasPermission(Permissions.ManageSchedule)]
    public async Task<IActionResult> SubstituteDoctorForSession(
    Guid packageId, [FromBody] SubstituteDoctorForSessionRequest request, CancellationToken cancellationToken)
    {
        var ownershipCheck = await EnsurePackageBelongsToCallerAsync(packageId, cancellationToken);
        if (ownershipCheck is not null) return ownershipCheck;

        var actingUserId = Guid.Parse(User.GetUserId()!);

        // 1. Flip the session's actual doctor (SessionModule's concern)
        var updateResult = await _sessionAssignmentService.UpdateSessionDoctorAsync(
            request.SessionId, request.NewDoctorId, cancellationToken);
        if (updateResult.IsFailure)
            return updateResult.ToProblem();

        // 2. Record the audit entry (PackageModule's concern)
        var historyResult = await _packageService.SubstituteDoctorForSessionAsync(
            packageId, request, actingUserId, cancellationToken);

        if (historyResult.IsFailure)
            return historyResult.ToProblem();

        return NoContent();
    }

    /// <summary>
    /// Full reassignment — ClinicAdmin only. No ownership check against the caller's
    /// own doctor id, since a ClinicAdmin manages doctors, not a single doctor's own
    /// packages. Enforced entirely by the ClinicAdmin-only permission below.
    /// </summary>
    [HttpPost("{packageId:guid}/reassign-doctor")]
    [HasPermission(Permissions.ReassignPackageDoctor)]   // NEW permission — ClinicAdmin only
    public async Task<IActionResult> ReassignDoctor(
        Guid packageId, [FromBody] ReassignPackageDoctorRequest request, CancellationToken cancellationToken)
    {
        var actingUserId = Guid.Parse(User.GetUserId()!);
        var result = await _packageService.ReassignPackageDoctorAsync(packageId, request, actingUserId, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpGet("{packageId:guid}/assignment-history")]
    [HasPermission(Permissions.ManageSchedule)]
    public async Task<IActionResult> GetAssignmentHistory(Guid packageId, CancellationToken cancellationToken)
    {
        var ownershipCheck = await EnsurePackageBelongsToCallerAsync(packageId, cancellationToken);
        if (ownershipCheck is not null) return ownershipCheck;

        var result = await _packageService.GetAssignmentHistoryAsync(packageId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    private async Task<IActionResult?> EnsurePackageBelongsToCallerAsync(Guid packageId, CancellationToken cancellationToken)
    {
        var doctorId = await User.GetDoctorIdAsync(_dbContext, cancellationToken);
        if (doctorId is null)
            return Result.Failure(ReceptionistErrors.DoctorNotResolved).ToProblem();

        var packageDoctorId = await _packageService.GetPackageDoctorIdAsync(packageId, cancellationToken);
        if (packageDoctorId is null)
            return Result.Failure(ReceptionistErrors.PackageNotFound).ToProblem();

        if (packageDoctorId.Value != doctorId.Value)
            return Result.Failure(ReceptionistErrors.PackageAccessDenied).ToProblem();

        return null;
    }
}
