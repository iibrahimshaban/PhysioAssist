using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;

namespace PhysioAssist.Api.Modules.Intake.Services;

public interface IDynamicFormValidationService
{
    Result ValidateSchema(DynamicFormSchemaDto schema);
    Result ValidateSubmissionAgainstSchema(DynamicFormSchemaDto schema, DynamicFormSubmissionDto submission);
}
