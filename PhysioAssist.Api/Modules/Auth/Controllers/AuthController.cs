using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Contracts;
using PhysioAssist.Api.Modules.Auth.Errors;

namespace PhysioAssist.Api.Modules.Auth.Controllers;

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
        return Result.Failure(UserErrors.InvalidCredentials).ToProblem();
    }
}
