using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.EntityConfiguration;

public class PreVisitIntakeConfiguration : IEntityTypeConfiguration<PreVisitIntake>
{
    public void Configure(EntityTypeBuilder<PreVisitIntake> builder)
    {
        builder.ToTable("PreVisitIntake", schema: "intake");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.DoctorId)
               .IsRequired();

        builder.Property(p => p.FormSchemaId)
               .IsRequired();

        builder.Property(p => p.FormSchemaVersion)
               .IsRequired();

        builder.Property(p => p.PatientName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(p => p.PatientEmail)
               .HasMaxLength(200);

        builder.Property(p => p.PatientPhone)
               .HasMaxLength(20);

        builder.Property(p => p.FormSubmissionData)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(p => p.PainPointsData)
               .HasColumnType("nvarchar(max)")
               .IsRequired(false);

        builder.Property(p => p.Status)
               .IsRequired()
               .HasConversion<int>()
               .HasDefaultValue(IntakeStatus.Pending);

        builder.Property(p => p.ConvertedToPatientId)
               .IsRequired(false);

        builder.Property(p => p.AccessTokenHash)
               .HasMaxLength(256);

        builder.Property(p => p.ExpiresAt)
               .IsRequired(false);

        builder.Property(p => p.SubmittedAt)
               .IsRequired()
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.ReviewedAt)
               .IsRequired(false);

        builder.Property(p => p.ReviewedByDoctorId)
               .IsRequired(false);

        builder.HasOne(p => p.FormSchema)
               .WithMany()
               .HasForeignKey(p => p.FormSchemaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.DoctorId, p.Status, p.SubmittedAt })
               .HasDatabaseName("IX_PreVisitIntake_DoctorId_Status_SubmittedAt");

        builder.HasIndex(p => p.FormSchemaId)
               .HasDatabaseName("IX_PreVisitIntake_FormSchemaId");

        builder.HasIndex(p => p.AccessTokenHash)
               .HasDatabaseName("IX_PreVisitIntake_AccessTokenHash");

        builder.HasIndex(p => p.ConvertedToPatientId)
               .HasDatabaseName("IX_PreVisitIntake_ConvertedToPatientId");
    }
}
