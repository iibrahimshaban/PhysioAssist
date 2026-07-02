using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.EntityConfiguration;

public class PatientFormSchemaConfiguration : IEntityTypeConfiguration<PatientFormSchema>
{
    public void Configure(EntityTypeBuilder<PatientFormSchema> builder)
    {
        builder.ToTable("PatientFormSchema", schema: "intake");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.DoctorId)
               .IsRequired();

        builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(p => p.Description)
               .HasMaxLength(500);

        builder.Property(p => p.SchemaJson)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Version)
               .IsRequired()
               .HasDefaultValue(1);

        builder.Property(p => p.Status)
               .IsRequired()
               .HasConversion<int>()
               .HasDefaultValue(FormSchemaStatus.Draft);

        builder.Property(p => p.IsDefault)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(p => p.SchemaHash)
               .IsRequired()
               .HasMaxLength(128);

        builder.Property(p => p.PublishedAt)
               .IsRequired(false);

        builder.HasIndex(p => new { p.DoctorId, p.IsDefault })
               .HasDatabaseName("IX_PatientFormSchema_DoctorId_IsDefault");

        builder.HasIndex(p => new { p.DoctorId, p.Status })
               .HasDatabaseName("IX_PatientFormSchema_DoctorId_Status");

        builder.HasIndex(p => new { p.DoctorId, p.Name })
               .HasDatabaseName("IX_PatientFormSchema_DoctorId_Name");
    }
}
