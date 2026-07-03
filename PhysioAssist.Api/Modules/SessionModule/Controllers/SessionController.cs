using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.SessionModule.Contracts;
using PhysioAssist.Api.Modules.SessionModule.Services;

namespace PhysioAssist.Api.Modules.SessionModule.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController(ISessionService sessionService) : ControllerBase
    {
        private readonly ISessionService _sessionService = sessionService;

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var result = await _sessionService.CreateSessionAsync(request);

            if (!result.IsSuccess)
                return result.ToProblem();

            return CreatedAtAction(
                nameof(GetSessionById),
                new { id = result.Value.Id },
                result.Value
            );
        }
        [HttpGet("{id}")]
        public IActionResult GetSessionById(Guid id)
        {
            return Ok();
        }
        [HttpPut("{id}/start")]
        public async Task<IActionResult> StartSession(Guid id)
        {
            var result = await _sessionService.StartSessionAsync(id);

            return result.IsSuccess
                ? NoContent()
                : result.ToProblem();
        }
    }
}
