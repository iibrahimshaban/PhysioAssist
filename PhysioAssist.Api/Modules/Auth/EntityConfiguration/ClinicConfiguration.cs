using PhysioAssist.Api.Modules.Auth.Entities;

namespace PhysioAssist.Api.Modules.Auth.EntityConfiguration;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("Clinic", schema: "auth");

        builder.Property(x => x.ClinicName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ClinicAddress).HasMaxLength(300);
    }
}
