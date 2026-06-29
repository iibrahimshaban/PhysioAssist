using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.EntityConfiguration;

public class PatientFormSchemaConfiguration : IEntityTypeConfiguration<PatientFormSchema>
{
    public void Configure(EntityTypeBuilder<PatientFormSchema> builder)
    {
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.ToTable("PatientFormSchema", schema: "intake");

        builder.Property(p => p.SchemaJson)
               .HasColumnType("nvarchar(max)");
    }
}
