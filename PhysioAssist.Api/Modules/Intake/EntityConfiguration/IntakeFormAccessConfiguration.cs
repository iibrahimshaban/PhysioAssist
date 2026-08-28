using PhysioAssist.Api.Modules.Intake.Entities;

namespace PhysioAssist.Api.Modules.Intake.EntityConfiguration;

public class IntakeFormAccessConfiguration : IEntityTypeConfiguration<IntakeFormAccess>
{
    public void Configure(EntityTypeBuilder<IntakeFormAccess> builder)
    {
        builder.ToTable("IntakeFormAccess", schema: "intake");
    }
}
