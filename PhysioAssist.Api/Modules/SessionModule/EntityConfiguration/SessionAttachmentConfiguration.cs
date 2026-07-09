using PhysioAssist.Api.Modules.SessionModule.Entities;

namespace PhysioAssist.Api.Modules.SessionModule.EntityConfiguration
{
    public class SessionAttachmentConfiguration : IEntityTypeConfiguration<SessionAttachment>
    {
        public void Configure(EntityTypeBuilder<SessionAttachment> builder)
        {
            builder.Property(a => a.Id)
                .ValueGeneratedNever();

            builder.ToTable("SessionAttachment", schema: "session");

            builder.Property(a => a.FileUrl)
                .HasMaxLength(500);

            builder.Property(a => a.FileType)
                .HasMaxLength(50);

            builder.Property(a => a.FileName)
                .HasMaxLength(200);

            builder.HasOne(a => a.Session)
    .WithMany(s => s.Attachments)
    .HasForeignKey(a => a.SessionId)
    .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
