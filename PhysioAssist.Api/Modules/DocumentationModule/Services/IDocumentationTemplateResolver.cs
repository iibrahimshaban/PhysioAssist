using PhysioAssist.Api.Modules.DocumentationModule.Contracts;
using System.Text.Json.Nodes;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public interface IDocumentationTemplateResolver
{
    Task<List<DocumentationTemplateSummaryResponse>> GetTemplatesAsync(PatientCategory? category = null);
    Task<Result<JsonArray>> GetAllFieldsAsync(Guid documentationTemplateId);
    Task<Result<JsonArray>> GetEffectiveFieldsAsync(Guid doctorId, Guid documentationTemplateId);
    Task<Result> SaveHiddenFieldsAsync(Guid doctorId, Guid documentationTemplateId, List<string> hiddenFieldIds);
}
