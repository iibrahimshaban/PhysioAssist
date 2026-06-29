using PhysioAssist.Api.Modules.SessionModule.Entities;

namespace PhysioAssist.Api.Modules.SessionModule.EntityConfiguration;

public class SessionTranscriptionConfiguration : IEntityTypeConfiguration<SessionTranscription>
{
    public void Configure(EntityTypeBuilder<SessionTranscription> builder)
    {
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.ToTable("SessionTranscription", schema: "session");

        builder.Property(t => t.RawTranscript)
               .HasColumnType("nvarchar(max)");

        builder.Property(t => t.EditedTranscript)
               .HasColumnType("nvarchar(max)");

        builder.Property(t => t.AudioFileUrl)
               .HasMaxLength(500);

        builder.Property(t => t.Language)
               .HasConversion<int>();

        builder.Property(t => t.Status)
               .HasConversion<int>();

        builder.HasOne(t => t.Session)
               .WithOne(s => s.Transcription)
               .HasForeignKey<SessionTranscription>(t => t.SessionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
