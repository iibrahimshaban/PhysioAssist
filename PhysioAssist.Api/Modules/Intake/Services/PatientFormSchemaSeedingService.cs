using PhysioAssist.Api.Modules.Intake.DTOs.FormSchemas;
using PhysioAssist.Api.Modules.Intake.Helpers;
using PhysioAssist.Api.Shared.Interfaces.Exposed;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhysioAssist.Api.Modules.Intake.Services;

public class PatientFormSchemaSeedingService(IIntakeService intakeService) : IPatientFormSchemaSeedingService
{
    private readonly IIntakeService _intakeService = intakeService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<Result> SeedDefaultSchemaAsync(Guid doctorId, string clinicName, CancellationToken cancellationToken = default)
    {
        var existingDefault = await _intakeService.GetDefaultFormSchemaAsync(doctorId, cancellationToken);
        if (existingDefault.IsSuccess)
            return Result.Success();

        var schemaDto = DefaultIntakeSchemaTemplate.Build();
        var schemaJson = JsonSerializer.Serialize(schemaDto, JsonOptions);

        var createRequest = new CreateFormSchemaRequest
        {
            Name = string.IsNullOrWhiteSpace(clinicName)
                ? "Default Intake Form"
                : $"{clinicName} - Form",
            Description = "welecome to our clinic, please fill out the form",
            SchemaJson = schemaJson,
            IsDefault = true,
        };

        var createResult = await _intakeService.CreateFormSchemaAsync(createRequest, doctorId, cancellationToken);
        if (createResult.IsFailure)
            return Result.Failure(createResult.Error);

        var publishRequest = new PublishFormSchemaRequest { Version = createResult.Value.Version };
        var publishResult = await _intakeService.PublishFormSchemaAsync(
            createResult.Value.Id, publishRequest, doctorId, cancellationToken);

        return publishResult.IsFailure ? Result.Failure(publishResult.Error) : Result.Success();
    }
}
