using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DoctorSchedulingPreferencesController(IDoctorSchedulingPreferenceService _service) : ControllerBase
{

    [HttpGet]
    [HasPermission(Permissions.ReadWorkingSchedule)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _service.GetForDoctorAsync(doctorId, ct);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    [HttpPut]
    [HasPermission(Permissions.WriteWorkingSchedule)]
    public async Task<IActionResult> Update([FromBody] UpdateDoctorSchedulingPreferenceRequest request, CancellationToken ct)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _service.UpdateForDoctorAsync(doctorId, request, ct);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
}
