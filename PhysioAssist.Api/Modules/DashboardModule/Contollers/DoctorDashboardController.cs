using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.DashboardModule.Services;
using System.Security.Claims;

namespace PhysioAssist.Api.Modules.DashboardModule.Contollers;

[Route("api/[controller]")]
[ApiController]
public class DoctorDashboardController(IDoctorDashboardService _doctorDashboardService) : ControllerBase
{
    [HttpGet("summary")]
    [HasPermission(Permissions.ReadDashboard)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var clinicId = User.GetClinicId();
        var doctorFirstName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;

        var result = await _doctorDashboardService.GetSummaryAsync(clinicId!.Value, doctorFirstName, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
