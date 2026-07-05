using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Contracts.Authentication;
using PhysioAssist.Api.Modules.PatientModule.DTOs;
using PhysioAssist.Api.Modules.PatientModule.Services;

namespace PhysioAssist.Api.Modules.PatientModule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController(IPatientService patientService) : ControllerBase
    {
        private readonly IPatientService _patientService = patientService;

        [HttpGet]
        public async Task<IActionResult> GetAllPatients()
        {
            var result = await _patientService.GetAllAsync();
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatientById(int id)
        {
            var result = await _patientService.GetByIdAsync(id);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPost]
        public async Task<IActionResult> CreatePatient([FromBody] PatientRequest request)
        {
            var result = await _patientService.CreateAsync(request);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id,[FromBody] PatientRequest request)
        {
            var result = await _patientService.UpdateAsync(id,request);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var result = await _patientService.DeleteAsync(id);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] PatientStatus status)
        {
            var result = await _patientService.UpdateStatusAsync(id, status);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }


        // pat doc 

        [HttpPost("{patientId}/assign/{doctorId}")]
        public async Task<IActionResult> AssignPatient(Guid patientId, Guid doctorId)
        {
            var result = await _patientService.AssignPatientAsync(doctorId, patientId);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpPut("{patientId}/discharge/{doctorId}")]
        public async Task<IActionResult> DischargePatient(Guid patientId, Guid doctorId)
        {
            var result = await _patientService.DischargePatientAsync(doctorId, patientId);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpPut("{patientId}/set-primary/{doctorId}")]
        public async Task<IActionResult> SetPrimaryDoctor(Guid patientId, Guid doctorId)
        {
            var result = await _patientService.SetPrimaryDoctorAsync(doctorId, patientId);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }
    }
}
