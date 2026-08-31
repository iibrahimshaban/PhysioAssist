using PhysioAssist.Api.Modules.PackageModule.Entities;

namespace PhysioAssist.Api.Modules.PackageModule.EntityConfiguration;

public class PatientSessionPackageConfiguration : IEntityTypeConfiguration<PatientSessionPackage>
{
    public void Configure(EntityTypeBuilder<PatientSessionPackage> builder)
    {
        builder.ToTable("PatientSessionPackages" , "package");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status)
            .HasConversion<int>();

        builder.Property(p => p.SessionDuration)
            .IsRequired();

        builder.Property(p => p.Priority)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne<TreatmentSchedulePlan>()
        .WithMany()
        .HasForeignKey(p => p.TreatmentSchedulePlanId)
        .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.TreatmentSchedulePlanId);

        // Useful for the background job that scans for packages needing the next batch scheduled.
        builder.HasIndex(p => new { p.DoctorId, p.Status });
        builder.HasIndex(p => new { p.PatientId, p.Status });

        // No navigation to Patient/Doctor — plain Guid FKs, same cross-module boundary rule as elsewhere.
    }
}