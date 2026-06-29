using PhysioAssist.Api.Shared.Entities;

namespace PhysioAssist.Api.Shared.EntityConfiguration;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notification", schema: "shared");

        builder.Property(n => n.Id)
               .ValueGeneratedNever();

        builder.Property(n => n.Channel)
               .HasMaxLength(50);

        builder.Property(n => n.Type)
               .HasMaxLength(100);

        builder.Property(n => n.Status)
               .HasMaxLength(50);
    }
}
