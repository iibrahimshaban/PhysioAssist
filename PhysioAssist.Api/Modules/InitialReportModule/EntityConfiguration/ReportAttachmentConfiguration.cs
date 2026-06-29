using PhysioAssist.Api.Modules.InitialReportModule.Entities;

namespace PhysioAssist.Api.Modules.InitialReportModule.EntityConfiguration;

public class ReportAttachmentConfiguration : IEntityTypeConfiguration<ReportAttachment>
{
    public void Configure(EntityTypeBuilder<ReportAttachment> builder)
    {
        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.ToTable("ReportAttachment", schema: "initialreport");

        builder.Property(a => a.FileUrl)
               .HasMaxLength(500);

        builder.Property(a => a.FileType)
               .HasMaxLength(50);

        builder.Property(a => a.FileName)
               .HasMaxLength(200);
    }
}
