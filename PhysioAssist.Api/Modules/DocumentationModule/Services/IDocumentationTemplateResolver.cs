using System.Text.Json.Nodes;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public interface IDocumentationTemplateResolver
{
    Task<Result<JsonArray>> GetAllFieldsAsync(Guid documentationTemplateId);
    Task<Result<JsonArray>> GetEffectiveFieldsAsync(Guid doctorId, Guid documentationTemplateId);
    Task<Result> SaveHiddenFieldsAsync(Guid doctorId, Guid documentationTemplateId, List<string> hiddenFieldIds);
}
