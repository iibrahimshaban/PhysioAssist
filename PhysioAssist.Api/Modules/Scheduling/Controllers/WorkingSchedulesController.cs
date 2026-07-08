using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.ResultPattern;

namespace PhysioAssist.Api.Modules.Scheduling.Controllers
{
    /// <summary>
    /// Manages a doctor's recurring weekly availability (WorkingSchedule + WorkingScheduleDays).
    /// This controller defines WHEN a doctor works — it has no knowledge of individual
    /// appointments. Booking, cancelling, and availability calculations live in
    /// <see cref="AppointmentsController"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WorkingSchedulesController(IWorkingScheduleService workingScheduleService) : ControllerBase
    {
        private readonly IWorkingScheduleService _workingScheduleService = workingScheduleService;


        /// <summary>
        /// Creates a new recurring working schedule for a doctor.
        /// </summary>
        /// <remarks>
        /// Business rules enforced:
        /// - A doctor can only have one active WorkingSchedule at a time. Attempting to create
        ///   a second one while an active schedule exists will fail — deactivate the existing
        ///   schedule first via <see cref="Deactivate"/>.
        /// - At least one working day must be provided.
        /// - Each day's end time must be after its start time.
        /// - The same weekday cannot appear more than once in the request.
        /// - A day the clinic is closed on (e.g. Friday) simply has no entry — there is no
        ///   explicit "closed" flag to set.
        /// </remarks>
        /// <param name="request">The doctor ID and the list of weekly working-day windows.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">Working schedule created successfully.</response>
        /// <response code="400">Request is invalid (e.g. no days, duplicate day, end before start).</response>
        /// <response code="409">Doctor already has an active working schedule.</response>
        [HttpPost]
        [ProducesResponseType(typeof(WorkingScheduleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<WorkingScheduleDto>> Create(
            [FromBody] CreateWorkingScheduleRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _workingScheduleService.CreateAsync(request, cancellationToken);

            if (result.IsFailure)
                return result.ToProblem();

            return CreatedAtAction(nameof(GetActiveByDoctor), new { doctorId = result.Value.DoctorId }, result.Value);
        }

        /// <summary>
        /// Retrieves a doctor's currently active working schedule, including its weekly day windows.
        /// </summary>
        /// <param name="doctorId">The doctor's ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Returns the active working schedule.</response>
        /// <response code="404">The doctor has no active working schedule.</response>
        [HttpGet("doctor/{doctorId:guid}")]
        [ProducesResponseType(typeof(WorkingScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkingScheduleDto>> GetActiveByDoctor(Guid doctorId, CancellationToken cancellationToken)
        {
            var result = await _workingScheduleService.GetActiveByDoctorAsync(doctorId, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }

        /// <summary>
        /// Replaces the entire set of working days for an existing working schedule.
        /// </summary>
        /// <remarks>
        /// This is a full replace, not a partial patch — the existing day list is cleared and
        /// rebuilt from the request. Already-booked appointments are never touched by this
        /// operation, even if they now fall outside the newly updated hours; that is treated
        /// as an accepted historical exception rather than something this endpoint must fix.
        /// </remarks>
        /// <param name="id">The WorkingSchedule ID to update.</param>
        /// <param name="request">The full new set of weekly working-day windows.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Working days updated successfully.</response>
        /// <response code="400">Request is invalid (e.g. no days, duplicate day, end before start).</response>
        /// <response code="404">No WorkingSchedule exists with the given ID.</response>
        [HttpPut("{id:guid}/days")]
        [ProducesResponseType(typeof(WorkingScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkingScheduleDto>> UpdateDays(
            Guid id,
            [FromBody] UpdateWorkingScheduleDaysRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _workingScheduleService.UpdateDaysAsync(id, request, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }


        /// <summary>
        /// Deactivates a working schedule, allowing a new one to be created for the same doctor.
        /// </summary>
        /// <remarks>
        /// This is a soft deactivation, not a delete — the schedule row remains for history.
        /// </remarks>
        /// <param name="id">The WorkingSchedule ID to deactivate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Working schedule deactivated successfully.</response>
        /// <response code="404">No WorkingSchedule exists with the given ID.</response>
        [HttpPost("{id:guid}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        {
            var result = await _workingScheduleService.DeactivateAsync(id, cancellationToken);

            return result.IsFailure ? result.ToProblem() : NoContent();
        }

        /// <summary>
        /// Permanently deletes a working schedule and its associated working days.
        /// </summary>
        /// <remarks>
        /// This is a hard delete, unlike <see cref="Deactivate"/> — the row is removed
        /// entirely and no history is kept. Prefer <see cref="Deactivate"/> for the normal
        /// "doctor stopped using this schedule" flow; use this only when the schedule
        /// should be erased outright (e.g. it was created by mistake).
        /// </remarks>
        /// <param name="id">The WorkingSchedule ID to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="204">Working schedule deleted successfully.</response>
        /// <response code="404">No WorkingSchedule exists with the given ID.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _workingScheduleService.DeleteAsync(id, cancellationToken);

            return result.IsFailure ? result.ToProblem() : NoContent();
        }
    }
}