using PhysioAssist.Api.Modules.PackageModule.Entities;

namespace PhysioAssist.Api.Modules.PackageModule.EntityConfiguration;

public class TreatmentSchedulePlanConfiguration : IEntityTypeConfiguration<TreatmentSchedulePlan>
{
    public void Configure(EntityTypeBuilder<TreatmentSchedulePlan> builder)
    {
        builder.ToTable("TreatmentSchedulePlans", "package");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Priority).HasConversion<int>();
        builder.Property(p => p.LifeCycleStatus).HasConversion<int>();
    }


}
