using PhysioAssist.Api.Modules.Auth.Entities;

namespace PhysioAssist.Api.Modules.Auth.EntityConfiguration;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.Property(e => e.Id)
               .ValueGeneratedNever();

        builder.ToTable("Doctor", schema: "auth");

        builder.Property(d => d.ClinicName)
               .HasMaxLength(200);
    }
}
