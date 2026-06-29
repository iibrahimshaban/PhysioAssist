using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.EntityConfiguration;

public class PreVisitIntakeConfiguration : IEntityTypeConfiguration<PreVisitIntake>
{
    public void Configure(EntityTypeBuilder<PreVisitIntake> builder)
    {
        builder.ToTable("PreVisitIntake", schema: "intake");

        builder.Property(p => p.PatientName)
               .HasMaxLength(100);

        builder.Property(p => p.FormSubmissionData)
               .HasColumnType("nvarchar(max)");

        builder.Property(p => p.PainPointsData)
               .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Status)
               .HasConversion<int>();
    }
}
