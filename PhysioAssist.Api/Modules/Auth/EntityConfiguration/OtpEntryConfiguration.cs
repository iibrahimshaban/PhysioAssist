using PhysioAssist.Api.Modules.Auth.Entities;

namespace PhysioAssist.Api.Modules.Auth.EntityConfiguration;

public class OtpEntryConfiguration : IEntityTypeConfiguration<OtpEntry>
{
    public void Configure(EntityTypeBuilder<OtpEntry> builder)
    {
        builder.ToTable("OtpEntry", schema: "auth");

        builder.Property(o => o.Id)
               .ValueGeneratedNever();

        builder.Property(o => o.Code)
               .HasMaxLength(10);

        builder.Property(o => o.Purpose)
               .HasConversion<int>();

        builder.HasOne(o => o.User)
               .WithMany(u => u.OtpEntries)
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
