using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Dtos.Schedule;
using PhysioAssist.Api.Shared.Extensions;
using PhysioAssist.Api.Shared.ResultPattern;

namespace PhysioAssist.Api.Modules.Scheduling.Controllers
{
    /// <summary>
    /// Manages patient appointments (ScheduleSlots) within the Scheduling module.
    /// An appointment always has a dynamic, doctor-decided duration — there are no
    /// pre-generated fixed-length slots in this system. Every endpoint here operates
    /// on an existing or new appointment; doctor working-hours configuration lives
    /// in <see cref="WorkingSchedulesController"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
    {
        private readonly IAppointmentService _appointmentService = appointmentService;

        /// <summary>
        /// Books a new appointment for a patient with a doctor.
        /// </summary>
        /// <remarks>
        /// Business rules enforced before creation:
        /// - Duration must be between 15 and 240 minutes.
        /// - Appointment cannot span across midnight into a different calendar day.
        /// - The doctor must have an active WorkingSchedule with a working day matching the requested weekday.
        /// - The requested time must fall entirely within that day's working hours.
        /// - The requested time must not overlap any existing Booked or Completed appointment for the same doctor.
        /// </remarks>
        /// <param name="request">Doctor, patient, and the requested start/end time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Appointment created successfully.</response>
        /// <response code="400">Request shape is invalid (e.g. end before start, duration out of bounds).</response>
        /// <response code="409">Requested time conflicts with working hours or an existing appointment.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ScheduleSlotDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ScheduleSlotDto>> Create([FromBody] CreateAppointmentRequest request,CancellationToken cancellationToken)
        {
            var result = await _appointmentService.CreateAsync(request, cancellationToken);

            if (result.IsFailure)
                return result.ToProblem();

            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        /// <summary>
        /// Retrieves a single appointment by its unique identifier.
        /// </summary>
        /// <param name="id">The appointment (ScheduleSlot) ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Appointment found and returned.</response>
        /// <response code="404">No appointment exists with the given ID.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ScheduleSlotDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ScheduleSlotDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.GetByIdAsync(id, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }

        /// <summary>
        /// Retrieves all Booked or Completed appointments for a doctor on a specific date.
        /// </summary>
        /// <remarks>
        /// Cancelled and NoShow appointments are excluded — this reflects the doctor's
        /// actual attended/scheduled workload for the day, not the full historical record.
        /// If <paramref name="doctorId"/> is omitted, it is resolved from the authenticated
        /// user's identity (the calling doctor's own appointments).
        /// </remarks>
        /// <param name="doctorId">The doctor's ID. Optional — falls back to the authenticated user.</param>
        /// <param name="date">The calendar date to query (e.g. 2026-07-04).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the list of appointments (may be empty).</response>
        /// <response code="401">No doctorId supplied and no valid doctor identity found on the request.</response>
        [HttpGet("doctor/{doctorId:guid?}")]
        public async Task<ActionResult<IReadOnlyList<ScheduleSlotDto>>> GetDoctorAppointments(Guid? doctorId,[FromQuery] DateTimeOffset date,CancellationToken cancellationToken)
        {
            var resolvedResult = ResolveDoctorId(doctorId);
            if (resolvedResult is null)
                return Unauthorized("No valid doctor identity found on the request.");

            var result = await _appointmentService.GetDoctorAppointmentsAsync(resolvedResult.Value, date, cancellationToken);
            return Ok(result);
        }



        /// <summary>
        /// Calculates the doctor's free (bookable) time intervals for a given date.
        /// </summary>
        /// <remarks>
        /// This is computed dynamically on every call — nothing is pre-generated or cached.
        /// If <paramref name="doctorId"/> is omitted, it is resolved from the authenticated
        /// user's identity.
        /// </remarks>
        /// <param name="doctorId">The doctor's ID. Optional — falls back to the authenticated user.</param>
        /// <param name="date">The calendar date to check availability for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the list of free intervals (may be empty if fully booked or a non-working day).</response>
        /// <response code="401">No doctorId supplied and no valid doctor identity found on the request.</response>
        [HttpGet("doctor/{doctorId:guid?}/availability")]
        [ProducesResponseType(typeof(IReadOnlyList<AvailableIntervalDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<AvailableIntervalDto>>> GetAvailability(Guid? doctorId,[FromQuery] DateTimeOffset date,CancellationToken cancellationToken)
        {
            var resolvedResult = ResolveDoctorId(doctorId);
            if (resolvedResult is null)
                return Unauthorized("No valid doctor identity found on the request.");

            var result = await _appointmentService.GetAvailabilityAsync(resolvedResult.Value, date, cancellationToken);
            return Ok(result);
        }


        /// <summary>
        /// Cancels a booked appointment, freeing the doctor's time for that slot.
        /// </summary>
        /// <remarks>
        /// Only appointments currently in <c>Booked</c> status can be cancelled.
        /// Cancelling does not delete the record — it's kept as <c>Cancelled</c> for history,
        /// and future overlap checks will ignore it, allowing the time to be rebooked.
        /// </remarks>
        /// <param name="id">The appointment ID to cancel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Appointment cancelled successfully.</response>
        /// <response code="404">No appointment exists with the given ID.</response>
        /// <response code="409">Appointment is not in a cancellable state (e.g. already Completed or Cancelled).</response>
        [HttpPost("{id:guid}/cancel")]
        [ProducesResponseType(typeof(ScheduleSlotDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ScheduleSlotDto>> Cancel(Guid id, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.CancelAsync(id, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }

        /// <summary>
        /// Reschedules an appointment to a new date/time.
        /// </summary>
        /// <remarks>
        /// Implemented as cancel-old + book-new, never an in-place time edit — this preserves
        /// an honest audit trail (you can see both the original and the rescheduled appointment
        /// in history) rather than silently overwriting when the appointment happened.
        /// The new time is validated exactly like a new booking (working hours + overlap),
        /// except it correctly excludes the original appointment from the overlap check.
        /// </remarks>
        /// <param name="id">The appointment ID to reschedule.</param>
        /// <param name="request">The new requested start/end time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the newly created replacement appointment.</response>
        /// <response code="404">No appointment exists with the given ID.</response>
        /// <response code="409">Appointment is not reschedulable, or the new time conflicts with working hours/another appointment.</response>
        [HttpPost("{id:guid}/reschedule")]
        [ProducesResponseType(typeof(ScheduleSlotDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ScheduleSlotDto>> Reschedule(Guid id,[FromBody] RescheduleAppointmentRequest request,CancellationToken cancellationToken)
        {
            var result = await _appointmentService.RescheduleAsync(id, request, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }

        /// <summary>
        /// Marks an appointment as completed, indicating the patient attended and the session finished.
        /// </summary>
        /// <param name="id">The appointment ID to complete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Appointment marked as completed.</response>
        /// <response code="404">No appointment exists with the given ID.</response>
        /// <response code="409">Appointment is not in a completable state (must currently be Booked).</response>
        [HttpPost("{id:guid}/complete")]
        [ProducesResponseType(typeof(ScheduleSlotDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ScheduleSlotDto>> Complete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.CompleteAsync(id, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }

        /// <summary>
        /// Marks an appointment as a no-show, indicating the patient did not attend.
        /// </summary>
        /// <param name="id">The appointment ID to mark.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Appointment marked as no-show.</response>
        /// <response code="404">No appointment exists with the given ID.</response>
        /// <response code="409">Appointment is not in a state that can be marked no-show (must currently be Booked).</response>
        [HttpPost("{id:guid}/no-show")]
        [ProducesResponseType(typeof(ScheduleSlotDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ScheduleSlotDto>> MarkNoShow(Guid id, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.MarkNoShowAsync(id, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }

        /// <summary>
        /// Permanently deletes an appointment record.
        /// </summary>
        /// <remarks>
        /// This is a hard delete, unlike <see cref="Cancel"/> — the row is removed entirely
        /// and no history is kept, regardless of the appointment's current status. Prefer
        /// <see cref="Cancel"/> for the normal "appointment called off" flow; use this only
        /// when the record itself should be erased (e.g. it was created by mistake).
        /// </remarks>
        /// <param name="id">The appointment ID to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Appointment deleted successfully.</response>
        /// <response code="404">No appointment exists with the given ID.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.DeleteAsync(id, cancellationToken);

            return result.IsFailure ? result.ToProblem() : NoContent();
        }


        /// <summary>
        /// Calculates the doctor's free (bookable) time intervals for every working day
        /// within a date range — one entry per working day, keyed by date.
        /// </summary>
        /// <remarks>
        /// If <paramref name="doctorId"/> is omitted, it is resolved from the authenticated
        /// user's identity. All other range rules are unchanged (see original remarks).
        /// </remarks>
        /// <param name="doctorId">The doctor's ID. Optional — falls back to the authenticated user.</param>
        /// <param name="from">Start of the range (inclusive). Optional — see remarks.</param>
        /// <param name="to">End of the range (inclusive). Optional — see remarks.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the list of daily availability entries.</response>
        /// <response code="400">Range is invalid.</response>
        /// <response code="401">No doctorId supplied and no valid doctor identity found on the request.</response>
        /// <response code="404">The doctor has no active working schedule.</response>
        [HttpGet("doctor/{doctorId:guid?}/availability-range")]
        [ProducesResponseType(typeof(IReadOnlyList<DailyAvailabilityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<DailyAvailabilityDto>>> GetAvailabilityRange(Guid? doctorId,[FromQuery] DateTimeOffset? from,[FromQuery] DateTimeOffset? to,CancellationToken cancellationToken)
        {
            var resolvedResult = ResolveDoctorId(doctorId);
            if (resolvedResult is null)
                return Unauthorized("No valid doctor identity found on the request.");

            var result = await _appointmentService.GetAvailabilityRangeAsync(resolvedResult.Value, from, to, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }



        /// <summary>
        /// Retrieves all cancelled appointments for a doctor, optionally filtered to a date range.
        /// </summary>
        /// <remarks>
        /// If <paramref name="doctorId"/> is omitted, it is resolved from the authenticated
        /// user's identity — previously this endpoint always overwrote a supplied doctorId
        /// with the caller's claim, making the route parameter unreachable; that's fixed here.
        /// </remarks>
        [HttpGet("doctor/{doctorId:guid?}/cancelled")]
        [ProducesResponseType(typeof(IReadOnlyList<ScheduleSlotDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<ScheduleSlotDto>>> GetCancelledAppointments(Guid? doctorId,[FromQuery] DateTimeOffset? from,[FromQuery] DateTimeOffset? to,CancellationToken cancellationToken)
        {
            var resolvedResult = ResolveDoctorId(doctorId);
            if (resolvedResult is null)
                return Unauthorized("No valid doctor identity found on the request.");

            var result = await _appointmentService.GetCancelledAppointmentsAsync(resolvedResult.Value, from, to, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }


        /// <summary>
        /// Resolves the effective doctor ID: the explicit route value if supplied,
        /// otherwise the authenticated user's own identity claim.
        /// </summary>
        private Guid? ResolveDoctorId(Guid? routeDoctorId)
        {
            if (routeDoctorId is not null)
                return routeDoctorId;

            var doctorIdClaim = User.GetUserId();
            return Guid.TryParse(doctorIdClaim, out var parsedId) ? parsedId : null;
        }

    }
}