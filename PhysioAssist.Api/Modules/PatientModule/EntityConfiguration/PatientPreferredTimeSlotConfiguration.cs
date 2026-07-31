using PhysioAssist.Api.Modules.PatientModule.Entities;

namespace PhysioAssist.Api.Modules.PatientModule.EntityConfiguration;

public class PatientPreferredTimeSlotConfiguration : IEntityTypeConfiguration<PatientPreferredTimeSlot>
{
    public void Configure(EntityTypeBuilder<PatientPreferredTimeSlot> builder)
    {
        builder.ToTable("PatientPreferredTimeSlot", schema: "patient");
    }
}
