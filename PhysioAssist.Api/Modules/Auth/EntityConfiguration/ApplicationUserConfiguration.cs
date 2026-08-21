using PhysioAssist.Api.Modules.Auth.Entities;

namespace PhysioAssist.Api.Modules.Auth.EntityConfiguration;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUser", schema: "auth");

        builder.Property(x => x.FirstName).HasMaxLength(100);

        builder.Property(x => x.LastName).HasMaxLength(100);

        builder.Property(x => x.ProfilePictureUrl).HasMaxLength(300);

        builder.HasOne(x => x.Clinic)
            .WithMany(c => c.Users)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(x => x.ClinicId)
            .HasDatabaseName("IX_ApplicationUser_ClinicId");
    }
}
