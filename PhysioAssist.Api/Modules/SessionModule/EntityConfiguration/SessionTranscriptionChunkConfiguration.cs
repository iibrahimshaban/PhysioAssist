using PhysioAssist.Api.Modules.SessionModule.Entities;

namespace PhysioAssist.Api.Modules.SessionModule.EntityConfiguration;

public class SessionTranscriptionChunkConfiguration : IEntityTypeConfiguration<SessionTranscriptionChunk>
{
    public void Configure(EntityTypeBuilder<SessionTranscriptionChunk> builder)
    {

        builder.ToTable("SessionTranscriptionChunk", schema: "session");

        builder.Property(c => c.ChunkText)
               .HasColumnType("nvarchar(max)");

        builder.Property(c => c.Embedding)
             .HasColumnType("VECTOR(1536)");

        builder.HasIndex(c => new { c.SessionTranscriptionId, c.ChunkIndex })
               .IsUnique();
    }
}
