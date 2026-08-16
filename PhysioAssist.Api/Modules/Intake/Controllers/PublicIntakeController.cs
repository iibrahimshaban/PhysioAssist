using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Intake.DTOs.Submissions;
using PhysioAssist.Api.Modules.Intake.Services;

namespace PhysioAssist.Api.Modules.Intake.Controllers;

[Route("api/public")]
[ApiController]
public class PublicIntakeController(IIntakeService intakeService) : ControllerBase
{
    private readonly IIntakeService _intakeService = intakeService;

    [HttpGet("intake")]
    public async Task<IActionResult> GetPublicForm([FromQuery] string token, CancellationToken cancellationToken)
    {
        var result = await _intakeService.GetPublicFormAsync(token, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("intake/submit")]
    public async Task<IActionResult> SubmitPublicIntake([FromQuery] string token, [FromBody] SubmitPreVisitIntakeRequest request, CancellationToken cancellationToken)
    {
        var result = await _intakeService.SubmitPublicIntakeAsync(token, request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
