using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.EntityConfiguration;

public class PreVisitIntakeConfiguration : IEntityTypeConfiguration<PreVisitIntake>
{
    public void Configure(EntityTypeBuilder<PreVisitIntake> builder)
    {
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.ToTable("PreVisitIntake", schema: "intake");

        builder.Property(p => p.PatientName)
               .HasMaxLength(200);

        builder.Property(p => p.PatientEmail)
               .HasMaxLength(200);

        builder.Property(p => p.PatientPhone)
               .HasMaxLength(20);

        builder.Property(p => p.FormSubmissionData)
               .HasColumnType("nvarchar(max)");

        builder.Property(p => p.PainPointsData)
               .HasColumnType("nvarchar(max)");

        builder.Property(p => p.AccessTokenHash)
               .HasMaxLength(256);

        builder.Property(p => p.Status)
               .HasConversion<int>();

        builder.HasIndex(p => new { p.DoctorId, p.Status, p.SubmittedAt });

        builder.HasIndex(p => p.FormSchemaId);

        builder.HasIndex(p => p.AccessTokenHash);

        builder.HasIndex(p => p.ConvertedToPatientId);
    }
}
