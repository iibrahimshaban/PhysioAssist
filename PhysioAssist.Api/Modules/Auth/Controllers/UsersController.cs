using Mapster;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Contracts.User;
using PhysioAssist.Api.Modules.Auth.Services;


namespace PhysioAssist.Api.Modules.Auth.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    [HttpGet("")]
    [HasPermission(Permissions.GetUsers)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync( cancellationToken);

        return Ok(users);
    }
    [HttpGet("{id}")]
    [HasPermission(Permissions.GetUsers)]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetDetailsAsync(id, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
    [HttpPost("")]
    [HasPermission(Permissions.CreateUsers)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { result.Value.Id }, result.Value)
            : result.ToProblem();
    }
    [HttpPost("receptionist")]
    [HasPermission(Permissions.CreateReceptionist)]
    public async Task<IActionResult> CreateReceptionist([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {

        var result = await _userService.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { result.Value.Id }, result.Value)
            : result.ToProblem();
    }
    [HttpPut("{id}")]
    [HasPermission(Permissions.UpdateUsers)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
    [HttpPut("{userId}/toggle-status")]
    [HasPermission(Permissions.UpdateUsers)]
    public async Task<IActionResult> ToggleStatus([FromRoute] string userId)
    {
        var result = await _userService.ToggleStatusAsync(userId);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
    [HttpPut("{id}/unlock")]
    [HasPermission(Permissions.UpdateUsers)]
    public async Task<IActionResult> UnLock([FromRoute] string id)
    {
        var result = await _userService.UnlockAsync(id);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
}
