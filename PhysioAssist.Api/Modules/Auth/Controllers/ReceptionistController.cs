using Mapster;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Auth.Contracts.Receptionist;
using PhysioAssist.Api.Modules.Auth.Contracts.User;
using PhysioAssist.Api.Modules.Auth.Services;

namespace PhysioAssist.Api.Modules.Auth.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReceptionistController(IReceptionistService _receptionistService) : ControllerBase
{

    [HttpGet]
    [HasPermission(Permissions.GetReceptionist)]
    public async Task<IActionResult> GetAll()
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _receptionistService.GetAllAsync(doctorId);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.GetReceptionist)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _receptionistService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("permissions")]
    public IActionResult GetAssignablePermissions()
    {
        var doctorPermissions = User.Claims
            .Where(c => c.Type == Permissions.Type)
            .Select(c => c.Value)
            .Distinct()
            .Where(Permissions.Metadata.ContainsKey)
            .Select(v => Permissions.Metadata[v]);

        return Ok(doctorPermissions);
    }

    [HttpPost]
    [HasPermission(Permissions.CreateReceptionist)]
    public async Task<IActionResult> Create([FromBody] CreateReceptionistRequest request, CancellationToken cancellationToken)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await _receptionistService.CreateAsync(doctorId, request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.UpdateReceptionist)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReceptionistRequest request, CancellationToken cancellationToken)
    {
        var result = await _receptionistService.UpdateAsync(id, request,cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{id:guid}/toggle-disabled")]
    [HasPermission(Permissions.UpdateReceptionist)]
    public async Task<IActionResult> ToggleDisabled(Guid id)
    {
        var result = await _receptionistService.ToggleDisabledAsync(id);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.UpdateReceptionist)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _receptionistService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
