using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WorkingSchedulesController(IWorkingScheduleService workingScheduleService, ApplicationDbContext _dbContext) : ControllerBase
    {
        private readonly IWorkingScheduleService _workingScheduleService = workingScheduleService;

        [HttpPost]
        [HasPermission(Permissions.WriteWorkingSchedule)]
        [ProducesResponseType(typeof(WorkingScheduleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<WorkingScheduleDto>> Create(
            [FromBody] CreateWorkingScheduleRequest request,
            CancellationToken cancellationToken)
        {
            var managingDoctorId = await User.GetDoctorIdAsync(_dbContext, cancellationToken);

            if (managingDoctorId is null)
                return Result.Failure(ReceptionistErrors.DoctorNotResolved).ToProblem();

            var effectiveRequest = new CreateWorkingScheduleRequest
            {
                DoctorId = managingDoctorId.Value,
                Days = request.Days
            };

            var result = await _workingScheduleService.CreateAsync(effectiveRequest, cancellationToken);

            if (result.IsFailure)
                return result.ToProblem();

            return CreatedAtAction(nameof(GetActiveByDoctor), new { doctorId = result.Value.DoctorId }, result.Value);
        }

        [HttpGet("doctor/{id:guid?}")]
        [HasPermission(Permissions.ReadWorkingSchedule)]
        [ProducesResponseType(typeof(WorkingScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkingScheduleDto>> GetActiveByDoctor(CancellationToken cancellationToken)
        {
            var managingDoctorId = await User.GetDoctorIdAsync(_dbContext, cancellationToken);

            if (managingDoctorId is null)
                return Result.Failure(ReceptionistErrors.DoctorNotResolved).ToProblem();

            var result = await _workingScheduleService.GetActiveByDoctorAsync(managingDoctorId.Value, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }

        [HttpPut("{id:guid}/days")]
        [HasPermission(Permissions.WriteWorkingSchedule)]
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

        [HttpPost("{id:guid}/deactivate")]
        [HasPermission(Permissions.WriteWorkingSchedule)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        {
            var result = await _workingScheduleService.DeactivateAsync(id, cancellationToken);

            return result.IsFailure ? result.ToProblem() : NoContent();
        }

        [HttpDelete("{id:guid}")]
        [HasPermission(Permissions.WriteWorkingSchedule)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _workingScheduleService.DeleteAsync(id, cancellationToken);

            return result.IsFailure ? result.ToProblem() : NoContent();
        }
    }
}