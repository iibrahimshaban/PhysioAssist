using PhysioAssist.Api.Modules.InitialReportModule.Entities;

namespace PhysioAssist.Api.Modules.InitialReportModule.EntityConfiguration;

public class InitialReportConfiguration : IEntityTypeConfiguration<InitialReport>
{
    public void Configure(EntityTypeBuilder<InitialReport> builder)
    {
        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.ToTable("InitialReport", schema: "initialreport");

        builder.Property(r => r.ReportText)
               .HasColumnType("nvarchar(max)");

        builder.Property(r => r.TreatmentPlanPdfUrl)
               .HasMaxLength(500);


    }
}
