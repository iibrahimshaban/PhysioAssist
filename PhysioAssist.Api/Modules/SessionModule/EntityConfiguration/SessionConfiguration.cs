using PhysioAssist.Api.Modules.SessionModule.Entities;

namespace PhysioAssist.Api.Modules.SessionModule.EntityConfiguration;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.ToTable("Session", schema: "session");

        builder.Property(s => s.SummaryText)
               .HasColumnType("nvarchar(max)");

        builder.Property(s => s.Status)
               .HasConversion<int>();
        builder
    .HasMany(s => s.Attachments)
    .WithOne(a => a.Session)
    .HasForeignKey(a => a.SessionId)
    .OnDelete(DeleteBehavior.Cascade);
    }
}
