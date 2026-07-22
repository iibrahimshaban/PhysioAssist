using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.EntityConfiguration;

public class PatientSessionPackageConfiguration : IEntityTypeConfiguration<PatientSessionPackage>
{
    public void Configure(EntityTypeBuilder<PatientSessionPackage> builder)
    {
        builder.ToTable("PatientSessionPackages" , "scheduling");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status)
            .HasConversion<int>();

        builder.Property(p => p.SessionDuration)
            .IsRequired();

        builder.Property(p => p.PreferredTimeOfDay)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.PreferredDays)
            .HasConversion<int>() // [Flags] enum stored as a single int column
            .IsRequired();

        builder.Property(p => p.Priority)
            .HasConversion<int>()
            .IsRequired();

        // Useful for the background job that scans for packages needing the next batch scheduled.
        builder.HasIndex(p => new { p.DoctorId, p.Status });
        builder.HasIndex(p => new { p.PatientId, p.Status });

        // No navigation to Patient/Doctor — plain Guid FKs, same cross-module boundary rule as elsewhere.
    }
}