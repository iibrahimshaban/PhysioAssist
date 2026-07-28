using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Authorization;
using PhysioAssist.Api.Shared.Dtos.Patient;
using PhysioAssist.Api.Shared.Dtos.Schedule;

namespace PhysioAssist.Api.Modules.Scheduling.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DoctorPatientForScheduleController(
        IPatientQueryService patientService,
        ITodaySessionsService _todaySessionsService,
        IPackageSchedulingStatusService _packageSchedulingStatusService,
        IPatientSessionPackageAdjustmentService _packageAdjustmentService,
        IPatientSessionSchedulingService _schedulingService,
        ApplicationDbContext _dbContext) : ControllerBase
    {
        private readonly IPatientQueryService _patientService = patientService;

        [HttpGet]
        [HasPermission(Permissions.GetPatients)]
        [ProducesResponseType(typeof(List<PatientResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<PatientResponse>>> GetAllPatientsForDoctor(
            CancellationToken cancellationToken)
        {
            var managingDoctorId = await User.GetDoctorIdAsync(_dbContext, cancellationToken);

            if (managingDoctorId is null)
                return Result.Failure(ReceptionistErrors.DoctorNotResolved).ToProblem();

            var result = await _patientService.GetAllPatientsForDoctorAsync(managingDoctorId.Value, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }

        [HttpGet("today-sessions")]
        [HasPermission(Permissions.DailySessions)]
        public async Task<IActionResult> GetTodaySessions(CancellationToken cancellationToken)
        {
            var doctorId = Guid.Parse(User.GetUserId()!);

            var result = await _todaySessionsService.GetTodaySessionsAsync(doctorId, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPost("{packageId:guid}/next-session-candidates")]
        [HasPermission(Permissions.ManageSchedule)]
        public async Task<IActionResult> GetNextSessionCandidates(
            Guid packageId,
            [FromBody] GetNextSessionCandidatesRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _schedulingService.GetNextSessionCandidatesAsync(
                packageId,
                request.PatientFreeTimeOverride,
                request.PersistFreeTimeOverride,
                request.SessionDurationOverride,
                request.SessionsPerWeekOverride,
                request.MinimumGapOverrideDays,
                request.PreferredTimeOfDayOverride,
                request.PreferredDaysOverride,
                cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("session/{sessionId:guid}/next-booking-context")]
        [HasPermission(Permissions.ReadSchedule)]
        public async Task<IActionResult> GetNextBookingContext(Guid sessionId, CancellationToken cancellationToken)
        {
            var result = await _packageSchedulingStatusService.GetContextAsync(sessionId, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPost("{packageId:guid}/extend")]
        [HasPermission(Permissions.ManageSchedule)]
        public async Task<IActionResult> ExtendPackage(Guid packageId, CancellationToken cancellationToken)
        {
            var result = await _packageAdjustmentService.ExtendByOneSessionAsync(packageId, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpPost("packages/{packageId:guid}/confirm-slot")]
        [HasPermission(Permissions.ManageSchedule)]
        public async Task<IActionResult> ConfirmSessionSlot(
            Guid packageId,
            [FromBody] SlotCandidateDto chosenSlot,
            CancellationToken cancellationToken)
        {
            var result = await _schedulingService.ConfirmSessionSlotAsync(packageId, chosenSlot, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
    }
}