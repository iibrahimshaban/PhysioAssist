using System.Text.Json;
using PhysioAssist.Api.Modules.Intake.Errors;

namespace PhysioAssist.Api.Modules.Intake.Services;

public class DynamicFormValidationService : IDynamicFormValidationService
{
    public Result ValidateSchemaJson(string schemaJson)
    {
        return IsValidJsonObject(schemaJson)
            ? Result.Success()
            : Result.Failure(IntakeErrors.InvalidSchema);
    }

    public Result ValidateSubmissionJson(string submissionJson)
    {
        return IsValidJsonObject(submissionJson)
            ? Result.Success()
            : Result.Failure(IntakeErrors.InvalidSubmission);
    }

    private static bool IsValidJsonObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
