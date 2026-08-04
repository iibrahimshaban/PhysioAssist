using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.DocumentationModule.Services;

namespace PhysioAssist.Api.Modules.DocumentationModule.Controller;

[ApiController]
[Route("api/patients/{patientId:guid}/documentation")]
[Authorize]
public class PatientDocumentationController(IPatientDocumentationOverviewService overviewService) : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(Guid patientId)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var sessions = await overviewService.GetSessionsAsync(doctorId, patientId);
        return Ok(sessions);
    }
}
