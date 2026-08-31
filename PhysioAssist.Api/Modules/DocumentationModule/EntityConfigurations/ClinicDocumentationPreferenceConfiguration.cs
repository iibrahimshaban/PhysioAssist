using PhysioAssist.Api.Modules.DocumentationModule.Entities;

namespace PhysioAssist.Api.Modules.DocumentationModule.EntityConfigurations;

public class ClinicDocumentationPreferenceConfiguration : IEntityTypeConfiguration<ClinicDocumentationPreference>
{
    public void Configure(EntityTypeBuilder<ClinicDocumentationPreference> builder)
    {
        builder.ToTable("ClinicDocumentationPreferences", "Documentation");

        builder.Property(p => p.HiddenFieldIds).HasMaxLength(1000);

        builder.HasIndex(p => new { p.ClinicId, p.DocumentationTemplateId }).IsUnique();

        builder.HasOne(p => p.Template)
            .WithMany()
            .HasForeignKey(p => p.DocumentationTemplateId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
