using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.EntityConfiguration;

public class PatientFormSchemaConfiguration : IEntityTypeConfiguration<PatientFormSchema>
{
    public void Configure(EntityTypeBuilder<PatientFormSchema> builder)
    {
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.ToTable("PatientFormSchema", schema: "intake");

        builder.Property(p => p.Name)
               .HasMaxLength(150);

        builder.Property(p => p.Description)
               .HasMaxLength(500);

        builder.Property(p => p.SchemaJson)
               .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Version);

        builder.Property(p => p.Status)
               .HasConversion<int>();

        builder.Property(p => p.SchemaHash)
               .HasMaxLength(128);

        builder.HasIndex(p => new { p.DoctorId, p.IsDefault });

        builder.HasIndex(p => new { p.DoctorId, p.Status });

        builder.HasIndex(p => new { p.DoctorId, p.Name });
    }
}
