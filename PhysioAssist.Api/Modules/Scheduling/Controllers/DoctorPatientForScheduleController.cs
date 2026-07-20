using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.PatientModule.Services;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces; // ASSUMPTION: adjust to wherever IPatientService actually lives
using PhysioAssist.Api.Shared.Dtos.Patient;
using PhysioAssist.Api.Shared.Extensions;
using PhysioAssist.Api.Shared.ResultPattern;

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
    public class DoctorPatientForScheduleController(IPatientQueryService patientService) : ControllerBase
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
            var doctorIdClaim = User.GetUserId();
            if (!Guid.TryParse(doctorIdClaim, out var doctorId))
                return Unauthorized("No valid doctor identity found on the request.");

            var result = await _patientService.GetAllPatientsForDoctorAsync(doctorId, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }
    }
}