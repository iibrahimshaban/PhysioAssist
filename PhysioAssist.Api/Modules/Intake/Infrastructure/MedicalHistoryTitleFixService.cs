using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;
using PhysioAssist.Api.Modules.Intake.Repositories;
using PhysioAssist.Api.Shared.Interfaces.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhysioAssist.Api.Modules.Intake.Infrastructure;

public class MedicalHistoryTitleFixService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MedicalHistoryTitleFixService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public MedicalHistoryTitleFixService(IServiceProvider services, ILogger<MedicalHistoryTitleFixService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Medical history title fix: checking existing schemas...");

        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPatientFormSchemaRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var schemas = await repo.GetAllAsync(cancellationToken);
        var found = 0;
        var updated = 0;

        foreach (var schema in schemas)
        {
            DynamicFormSchemaDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<DynamicFormSchemaDto>(schema.SchemaJson, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("Medical history title fix: skipping schema {SchemaId} — failed to parse SchemaJson: {Error}", schema.Id, ex.Message);
                continue;
            }

            if (dto is null) continue;

            var changed = false;
            var newSections = new List<FormSectionDto>();

            foreach (var section in dto.Sections)
            {
                if (string.IsNullOrWhiteSpace(section.Title) &&
                    section.Groups.Count == 1 &&
                    section.Groups[0].Title == "Medical History")
                {
                    newSections.Add(section with { Title = "Medical History" });
                    found++;
                    changed = true;
                }
                else
                {
                    newSections.Add(section);
                }
            }

            if (!changed) continue;

            dto = dto with { Sections = newSections };
            schema.SchemaJson = JsonSerializer.Serialize(dto, JsonOptions);
            repo.Update(schema);
            updated++;
        }

        if (updated > 0)
        {
            await uow.SaveAsync(cancellationToken);
        }

        _logger.LogInformation("Medical history title fix: found {Found}, updated {Updated}", found, updated);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}