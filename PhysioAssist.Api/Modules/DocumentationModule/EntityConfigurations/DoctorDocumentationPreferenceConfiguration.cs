using PhysioAssist.Api.Modules.DocumentationModule.Entities;

namespace PhysioAssist.Api.Modules.DocumentationModule.EntityConfigurations;

public class DoctorDocumentationPreferenceConfiguration : IEntityTypeConfiguration<DoctorDocumentationPreference>
{
    public void Configure(EntityTypeBuilder<DoctorDocumentationPreference> builder)
    {
        builder.ToTable("DoctorDocumentationPreferences", "Documentation");

        builder.Property(p => p.HiddenFieldIds).HasMaxLength(1000);

        builder.HasIndex(p => new { p.DoctorId, p.DocumentationTemplateId }).IsUnique();

        builder.HasOne(p => p.Template)
            .WithMany()
            .HasForeignKey(p => p.DocumentationTemplateId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
