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
        var doctorId = Guid.Parse(User.GetUserId()!);
        var doctorFirstName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;

        Console.WriteLine($"Doctor ID: {doctorId}, Doctor First Name: {doctorFirstName}");

        var result = await _doctorDashboardService.GetSummaryAsync(doctorId, doctorFirstName, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
