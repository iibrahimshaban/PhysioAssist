using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Scheduling.Services.Implementations;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces; // ASSUMPTION: adjust to wherever IPatientService actually lives
using PhysioAssist.Api.Shared.Dtos.Patient;
using PhysioAssist.Api.Shared.Dtos.Schedule;


namespace PhysioAssist.Api.Modules.Scheduling.Controllers
{
    /// <summary>
    /// Provides the list of patients associated with the currently authenticated doctor,
    /// for use when scheduling a new appointment (existing-patient search/select).
    /// This controller is read-only and has no knowledge of appointments or working schedules.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DoctorPatientForScheduleController(
        IPatientQueryService patientService, 
        ITodaySessionsService _todaySessionsService,
        IPackageSchedulingStatusService _packageSchedulingStatusService,
        IPatientSessionPackageAdjustmentService _packageAdjustmentService,
        IPatientSessionSchedulingService _schedulingService) : ControllerBase
    {
        private readonly IPatientQueryService _patientService = patientService;

        /// <summary>
        /// Retrieves all patients linked to the currently authenticated doctor.
        /// </summary>
        /// <remarks>
        /// Business rules enforced:
        /// - The DoctorId is NEVER accepted from the request — it is resolved server-side
        ///   from the authenticated user's identity claims, so a doctor can only ever see
        ///   their own patient list.
        /// </remarks>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the doctor's patients.</response>
        /// <response code="401">No authenticated user / identity claim missing.</response>
        /// <response code="404">The doctor has no linked patients.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<PatientResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<PatientResponse>>> GetAllPatientsForDoctor(
            CancellationToken cancellationToken)
        {
            var doctorId = Guid.Parse(User.GetUserId()!);

            var result = await _patientService.GetAllPatientsForDoctorAsync(doctorId, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }
        [HttpGet("today-sessions")]
        public async Task<IActionResult> GetTodaySessions(CancellationToken cancellationToken)
        {
            var doctorId = Guid.Parse(User.GetUserId()!);

            var result = await _todaySessionsService.GetTodaySessionsAsync(doctorId, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
        [HttpPost("{packageId:guid}/next-session-candidates")]
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
        public async Task<IActionResult> GetNextBookingContext(Guid sessionId, CancellationToken cancellationToken)
        {
            var result = await _packageSchedulingStatusService.GetContextAsync(sessionId, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPost("{packageId:guid}/extend")]
        public async Task<IActionResult> ExtendPackage(Guid packageId, CancellationToken cancellationToken)
        {
            var result = await _packageAdjustmentService.ExtendByOneSessionAsync(packageId, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }
        [HttpPost("packages/{packageId:guid}/confirm-slot")]
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