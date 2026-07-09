using PhysioAssist.Api.Modules.Auth.Entities;

namespace PhysioAssist.Api.Modules.Auth.EntityConfiguration;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.Property(e => e.Id)
               .ValueGeneratedNever();

        builder.ToTable("Doctor", schema: "auth");

        builder.Property(x => x.ClinicName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150);
        builder.Property(x => x.ClinicAddress).HasMaxLength(300);
        builder.Property(x => x.About).HasMaxLength(1000);
    }
}
