using PhysioAssist.Api.Modules.Auth.Entities;

namespace PhysioAssist.Api.Modules.Auth.EntityConfiguration;

public class ReceptionistConfiguration : IEntityTypeConfiguration<Receptionist>
{
    public void Configure(EntityTypeBuilder<Receptionist> builder)
    {
        builder.ToTable("Receptionists", schema: "auth");

        builder.Property(r => r.From).HasColumnType("time");
        builder.Property(r => r.To).HasColumnType("time");

        builder.HasOne(r => r.ManagingDoctor)
            .WithMany()
            .HasForeignKey(r => r.ManagingDoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
