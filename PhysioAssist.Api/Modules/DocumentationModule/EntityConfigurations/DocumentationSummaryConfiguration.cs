using PhysioAssist.Api.Modules.DocumentationModule.Entities;

namespace PhysioAssist.Api.Modules.DocumentationModule.EntityConfigurations;

public class DocumentationSummaryConfiguration : IEntityTypeConfiguration<DocumentationSummary>
{
    public void Configure(EntityTypeBuilder<DocumentationSummary> builder)
    {
        builder.ToTable("DocumentationSummaries", "Documentation");

        builder.Property(s => s.FocusAreas).HasMaxLength(1000);      // small JSON array of tags
        builder.Property(s => s.SummaryText).HasColumnType("nvarchar(max)"); // generated paragraph, length varies
        builder.Property(s => s.FileUrl).HasMaxLength(500);          // Cloudinary URL
    }
}
