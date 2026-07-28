using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Modules.PatientModule.DTOs;
using PhysioAssist.Api.Modules.PatientModule.Services;

namespace PhysioAssist.Api.Modules.PatientModule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PatientController(IPatientService patientService, IScheduleSlotQueryService _scheduleSlotQueryService, ApplicationDbContext _dbContext) : ControllerBase
    {
        private readonly IPatientService _patientService = patientService;

        [HttpGet]
        [HasPermission(Permissions.GetPatients)]
        public async Task<IActionResult> GetAllPatients()
        {
            var result = await _patientService.GetAllAsync();
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("{id}")]
        [HasPermission(Permissions.GetPatients)]
        public async Task<IActionResult> GetPatientById(Guid id)
        {
            var result = await _patientService.GetByIdAsync(id);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPost]
        [HasPermission(Permissions.WritePatient)]
        public async Task<IActionResult> CreatePatient([FromBody] PatientRequest request)
        {
            var result = await _patientService.CreateAsync(request);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.WritePatient)]
        public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] PatientRequest request)
        {
            var result = await _patientService.UpdateAsync(id, request);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.WritePatient)]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            var result = await _patientService.DeleteAsync(id);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpPut("{id}/status")]
        [HasPermission(Permissions.WritePatient)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] PatientStatus status)
        {
            var result = await _patientService.UpdateStatusAsync(id, status);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        // pat doc 

        //[HttpPost("{patientId}/assign/{doctorId}")]
        //[HasPermission(Permissions.WritePatient)]
        //public async Task<IActionResult> AssignPatient(Guid patientId, Guid doctorId)
        //{
        //    var result = await _patientService.AssignPatientAsync(doctorId, patientId);
        //    return result.IsSuccess ? NoContent() : result.ToProblem();
        //}

        //[HttpPut("{patientId}/discharge/{doctorId}")]
        //[HasPermission(Permissions.WritePatient)]
        //public async Task<IActionResult> DischargePatient(Guid patientId, Guid doctorId)
        //{
        //    var result = await _patientService.DischargePatientAsync(doctorId, patientId);
        //    return result.IsSuccess ? NoContent() : result.ToProblem();
        //}

        //[HttpPut("{patientId}/set-primary/{doctorId}")]
        //[HasPermission(Permissions.WritePatient)]
        //public async Task<IActionResult> SetPrimaryDoctor(Guid patientId, Guid doctorId)
        //{
        //    var result = await _patientService.SetPrimaryDoctorAsync(doctorId, patientId);
        //    return result.IsSuccess ? NoContent() : result.ToProblem();
        //}

        [HttpGet("with-slots")]
        [HasPermission(Permissions.GetPatients)]
        public async Task<IActionResult> GetWithSlots(CancellationToken ct)
        {
            var managingDoctorId = await User.GetDoctorIdAsync(_dbContext, ct);

            if (managingDoctorId is null)
                return Result.Failure(ReceptionistErrors.DoctorNotResolved).ToProblem();

            var result = await _patientService.GetPatientsWithSlotsAsync(managingDoctorId.Value, ct);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("{id}/overview")]
        [HasPermission(Permissions.GetPatients)]
        public async Task<IActionResult> GetOverview(Guid id, CancellationToken ct)
        {
            var result = await _patientService.GetPatientOverviewAsync(id, ct);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPut("{id}/overview/submission-data")]
        [HasPermission(Permissions.WritePatient)]
        public async Task<IActionResult> UpdateOverviewSubmissionData(Guid id, [FromBody] UpdateSubmissionDataRequest request, CancellationToken ct)
        {
            var result = await _patientService.UpdatePatientOverviewSubmissionAsync(
                id, request.FormSubmissionData, request.PainPointsData, ct);

            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpGet("{patientId:guid}/schedule-overview")]
        [HasPermission(Permissions.GetPatients)]
        public async Task<IActionResult> GetScheduleOverview(Guid patientId, CancellationToken cancellationToken)
        {
            var result = await _scheduleSlotQueryService.GetScheduleOverviewAsync(patientId, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPost("create-from-intake")]
        public async Task<IActionResult> CreateFromIntake([FromBody] CreateFromIntakeRequest request, CancellationToken ct)
        {
            var doctorId = Guid.Parse(User.GetUserId()!);
            var result = await _patientService.CreatePatientFromDynamicFormAsync(request.FormSchemaId, request.FormSubmissionData, request.PainPointsData, doctorId, ct);
            return result.IsSuccess ? Ok(new { patientId = result.Value }) : result.ToProblem();
        }
    }
}