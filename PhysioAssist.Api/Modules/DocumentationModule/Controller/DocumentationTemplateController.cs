using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.DocumentationModule.Contracts;
using PhysioAssist.Api.Modules.DocumentationModule.Services;
using PhysioAssist.Api.Shared.Extensions;

namespace PhysioAssist.Api.Modules.DocumentationModule.Controller;

[Route("api/[controller]")]
[ApiController]
public class DocumentationTemplateController(IDocumentationTemplateResolver resolver) : ControllerBase
{
    [HttpGet("{templateId:guid}/fields")]
    public async Task<IActionResult> GetAllFields(Guid templateId)
    {
        var result = await resolver.GetAllFieldsAsync(templateId);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    // Used by BOTH the progress-note form and the AI extraction service (called in-process there,
    // not via this endpoint) — this is the doctor-facing side of that same effective-fields logic.
    [HttpGet("{templateId:guid}/effective-fields")]
    public async Task<IActionResult> GetEffectiveFields(Guid templateId)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await resolver.GetEffectiveFieldsAsync(doctorId, templateId);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    // Settings screen only (write) — saves which field ids this doctor wants hidden for this template.
    [HttpPut("{templateId:guid}/hidden-fields")]
    public async Task<IActionResult> SaveHiddenFields(Guid templateId, [FromBody] SaveHiddenFieldsRequest request)
    {
        var doctorId = Guid.Parse(User.GetUserId()!);
        var result = await resolver.SaveHiddenFieldsAsync(doctorId, templateId, request.HiddenFieldIds);
        return result.IsFailure ? result.ToProblem() : Ok();
    }
}
