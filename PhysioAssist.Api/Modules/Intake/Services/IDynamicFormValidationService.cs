namespace PhysioAssist.Api.Modules.Intake.Services;

public interface IDynamicFormValidationService
{
    Result ValidateSchemaJson(string schemaJson);
    Result ValidateSubmissionJson(string submissionJson);
}
