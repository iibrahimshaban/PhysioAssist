using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Modules.PatientModule.DTOs;
using PhysioAssist.Api.Modules.PatientModule.Repositories;
using PhysioAssist.Api.Modules.PatientModule.Services;

namespace PhysioAssist.Api.Modules.PatientModule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class PatientController(
        IPatientService patientService,
        IScheduleSlotQueryService _scheduleSlotQueryService,
        IPatientQueryService _patientRepository
        ) : ControllerBase
    {
        private readonly IPatientService _patientService = patientService;

        [HttpGet]
        [HasPermission(Permissions.GetPatients)]
        public async Task<IActionResult> GetAllPatients()
        {
            var clinicId = User.GetClinicId();
            var result = await _patientService.GetAllAsync(clinicId!.Value);
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
            var clinicId = User.GetClinicId();
            var result = await _patientService.CreateAsync(clinicId!.Value, request);
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

        [HttpGet("with-slots")]
        [HasPermission(Permissions.GetPatients)]
        public async Task<IActionResult> GetWithSlots(CancellationToken ct)
        {
            var clinicId = User.GetClinicId();

            if (clinicId is null)
                return Result.Failure(ReceptionistErrors.DoctorNotResolved).ToProblem();

            var result = await _patientService.GetPatientsWithSlotsAsync(clinicId.Value, ct);
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
            var generatedByUserId = Guid.Parse(User.GetUserId()!);
            var clinicId = User.GetClinicId();
            var result = await _patientService.CreatePatientFromDynamicFormAsync(request.FormSchemaId, 
                request.FormSubmissionData, request.PainPointsData, generatedByUserId, clinicId, ct);

            return result.IsSuccess ? Ok(new { patientId = result.Value }) : result.ToProblem();
        }
        [HttpGet("patients/check-email")]
        [Authorize]
        public async Task<IActionResult> CheckPatientEmail([FromQuery] string email, CancellationToken cancellationToken)
        {
            var clinicId = User.GetClinicId(); // however you currently read the claim elsewhere
            var result = await _patientRepository.IsPatientEmailRegisteredAsync(email, clinicId!.Value, cancellationToken);

            return result.IsSuccess ? Ok(new { isRegistered = result.Value }) : result.ToProblem();
        }

        [HttpGet("patients/check-phone")]
        [Authorize]
        public async Task<IActionResult> CheckPatientPhone([FromQuery] string phoneNumber, CancellationToken cancellationToken)
        {
            var clinicId = User.GetClinicId();
            var result = await _patientRepository.IsPatientPhoneRegisteredAsync(phoneNumber, clinicId!.Value, cancellationToken);

            return result.IsSuccess ? Ok(new { isRegistered = result.Value }) : result.ToProblem();
        }
    }
}