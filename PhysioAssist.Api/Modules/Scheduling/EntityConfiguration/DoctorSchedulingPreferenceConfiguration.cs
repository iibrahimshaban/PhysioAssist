using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.EntityConfiguration;

public class DoctorSchedulingPreferenceConfiguration : IEntityTypeConfiguration<DoctorSchedulingPreference>
{
    public void Configure(EntityTypeBuilder<DoctorSchedulingPreference> builder)
    {
        builder.ToTable("DoctorSchedulingPreferences" , "scheduling");

        builder.HasKey(p => p.Id);

        // One preference row per doctor.
        builder.HasIndex(p => p.DoctorId).IsUnique();

        builder.Property(p => p.MaxShortfallTolerance)
            .IsRequired();

        builder.Property(p => p.MaxDaysOutForExactMatch)
            .IsRequired();

        builder.Property(p => p.AllowShorterSlots)
            .IsRequired();

    }
}
