using PhysioAssist.Api.Modules.DocumentationModule.Entities;
using PhysioAssist.Api.Persistence;

namespace PhysioAssist.Api.Modules.DocumentationModule.Seed;

public static class DocumentationTemplateSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.DocumentationTemplates.AnyAsync())
            return; // idempotent — don't reseed if templates already exist

        var templates = new List<DocumentationTemplate>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Category = PatientCategory.Neurological,
                Name = "Neurological Objective Findings v1",
                IsActive = true,
                SchemaJson = NeurologicalSchema.Json
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Category = PatientCategory.Orthopedic,
                Name = "Orthopedic Objective Findings v1",
                IsActive = true,
                SchemaJson = OrthopedicSchema.Json
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Category = PatientCategory.Pediatric,
                Name = "Pediatric Objective Findings v1",
                IsActive = true,
                SchemaJson = PediatricSchema.Json
            }
        };

        await context.DocumentationTemplates.AddRangeAsync(templates);
        await context.SaveChangesAsync();
    }
}
