using PhysioAssist.Api.Modules.PatientModule.Entities;

namespace PhysioAssist.Api.Modules.PatientModule.EntityConfiguration;

public class DoctorPatientConfiguration : IEntityTypeConfiguration<DoctorPatient>
{
    public void Configure(EntityTypeBuilder<DoctorPatient> builder)
    {
        builder.ToTable("DoctorPatient", schema: "patient");

        builder.HasKey(dp => new { dp.DoctorId, dp.PatientId });

        builder.Property(dp => dp.AccessLevel)
               .HasConversion<int>();

        builder.Property(dp => dp.Status)
               .HasConversion<int>();
    }
}
