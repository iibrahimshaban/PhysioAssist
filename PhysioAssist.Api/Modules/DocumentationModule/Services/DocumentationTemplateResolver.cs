using PhysioAssist.Api.Modules.DocumentationModule.Entities;
using PhysioAssist.Api.Modules.DocumentationModule.Errors;
using PhysioAssist.Api.Persistence;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PhysioAssist.Api.Modules.DocumentationModule.Services;

public class DocumentationTemplateResolver(ApplicationDbContext context) : IDocumentationTemplateResolver
{
    public async Task<Result<JsonArray>> GetAllFieldsAsync(Guid documentationTemplateId)
    {
        var template = await context.DocumentationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == documentationTemplateId);

        if (template is null)
            return Result.Failure<JsonArray>(DocumentationErrors.TemplateNotFound);

        var fieldsResult = ExtractFieldsArray(template.SchemaJson);
        if (fieldsResult.IsFailure)
            return Result.Failure<JsonArray>(fieldsResult.Error);

        return Result.Success(fieldsResult.Value);
    }

    public async Task<Result<JsonArray>> GetEffectiveFieldsAsync(Guid doctorId, Guid documentationTemplateId)
    {
        var allFieldsResult = await GetAllFieldsAsync(documentationTemplateId);
        if (allFieldsResult.IsFailure)
            return allFieldsResult;

        var preference = await context.DoctorDocumentationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.DoctorId == doctorId && p.DocumentationTemplateId == documentationTemplateId);

        if (preference?.HiddenFieldIds is null)
            return allFieldsResult; // no hidden fields configured — everything is effective

        var hiddenIds = JsonSerializer.Deserialize<List<string>>(preference.HiddenFieldIds) ?? [];
        if (hiddenIds.Count == 0)
            return allFieldsResult;

        var effectiveFields = new JsonArray();
        foreach (var field in allFieldsResult.Value)
        {
            var fieldId = field?["id"]?.GetValue<string>();
            if (fieldId is not null && hiddenIds.Contains(fieldId))
                continue;

            effectiveFields.Add(field?.DeepClone());
        }

        return Result.Success(effectiveFields);
    }

    public async Task<Result> SaveHiddenFieldsAsync(Guid doctorId, Guid documentationTemplateId, List<string> hiddenFieldIds)
    {
        var templateExists = await context.DocumentationTemplates
            .AnyAsync(t => t.Id == documentationTemplateId);

        if (!templateExists)
            return Result.Failure(DocumentationErrors.TemplateNotFound);

        var preference = await context.DoctorDocumentationPreferences
            .FirstOrDefaultAsync(p => p.DoctorId == doctorId && p.DocumentationTemplateId == documentationTemplateId);

        var serializedHiddenIds = JsonSerializer.Serialize(hiddenFieldIds);

        if (preference is null)
        {
            context.DoctorDocumentationPreferences.Add(new DoctorDocumentationPreference
            {
                Id = Guid.CreateVersion7(),
                DoctorId = doctorId,
                DocumentationTemplateId = documentationTemplateId,
                HiddenFieldIds = serializedHiddenIds
            });
        }
        else
        {
            preference.HiddenFieldIds = serializedHiddenIds;
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    private static Result<JsonArray> ExtractFieldsArray(string schemaJson)
    {
        var schemaNode = JsonNode.Parse(schemaJson);
        var fields = schemaNode?["fields"]?.AsArray();

        return fields is null
            ? Result.Failure<JsonArray>(DocumentationErrors.InvalidSchemaJson)
            : Result.Success(fields);
    }
}
