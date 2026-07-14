using PhysioAssist.Api.Modules.DocumentationModule.Entities;

namespace PhysioAssist.Api.Modules.DocumentationModule.EntityConfigurations;

public class SessionProgressNoteConfiguration : IEntityTypeConfiguration<SessionProgressNote>
{
    public void Configure(EntityTypeBuilder<SessionProgressNote> builder)
    {
        builder.ToTable("SessionProgressNotes", "Documentation");

        builder.Property(n => n.Subjective).HasColumnType("nvarchar(max)");        // free-text narrative
        builder.Property(n => n.ObjectiveFindings).HasColumnType("nvarchar(max)"); // JSON, unbounded
        builder.Property(n => n.Assessment).HasColumnType("nvarchar(max)");        // free-text narrative
        builder.Property(n => n.Plan).HasColumnType("nvarchar(max)");              // free-text narrative

        builder.HasOne(n => n.Template)
           .WithMany()
           .HasForeignKey(n => n.DocumentationTemplateId)
           .OnDelete(DeleteBehavior.NoAction);
    }
}
