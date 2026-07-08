using PhysioAssist.Api.Modules.DocumentationModule.Entities;

namespace PhysioAssist.Api.Modules.DocumentationModule.EntityConfigurations;

public class DocumentationTemplateConfiguration : IEntityTypeConfiguration<DocumentationTemplate>
{
    public void Configure(EntityTypeBuilder<DocumentationTemplate> builder)
    {
        builder.ToTable("DocumentationTemplates", "Documentation");

        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.SchemaJson).HasColumnType("nvarchar(max)"); // JSON schema, unbounded
    }
}
