using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Contracts.Auth;
using PhysioAssist.Api.Errors;

namespace PhysioAssist.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    [HttpPost("")]
    public IActionResult Login([FromBody]LoginRequest request)
    {
        return Ok(request);
    }
    [HttpGet("")]
    public IActionResult TestResult()
    {
        return Result.Failure(UserErrors.DisabledUser).ToProblem();
    }
}
