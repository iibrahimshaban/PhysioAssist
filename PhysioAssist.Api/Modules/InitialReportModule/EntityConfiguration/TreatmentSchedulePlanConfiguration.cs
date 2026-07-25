using PhysioAssist.Api.Modules.InitialReportModule.Entities;

namespace PhysioAssist.Api.Modules.InitialReportModule.EntityConfiguration;

public class TreatmentSchedulePlanConfiguration : IEntityTypeConfiguration<TreatmentSchedulePlan>
{
    public void Configure(EntityTypeBuilder<TreatmentSchedulePlan> builder)
    {
        builder.ToTable("TreatmentSchedulePlans", "initialreport");

        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Report)
               .WithOne(r => r.TreatmentSchedulePlan)
               .HasForeignKey<TreatmentSchedulePlan>(p => p.ReportId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.Property(p => p.PreferredTimeOfDay).HasConversion<int>();
        builder.Property(p => p.PreferredDays).HasConversion<int>();
        builder.Property(p => p.Priority).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();
    }


}
