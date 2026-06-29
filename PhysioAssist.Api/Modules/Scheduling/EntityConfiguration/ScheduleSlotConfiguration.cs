using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.EntityConfiguration;

public class ScheduleSlotConfiguration : IEntityTypeConfiguration<ScheduleSlot>
{
    public void Configure(EntityTypeBuilder<ScheduleSlot> builder)
    {
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.ToTable("ScheduleSlot", schema: "scheduling");

        builder.Property(s => s.Status)
               .HasConversion<int>();

        builder.HasIndex(s => new { s.DoctorId, s.SlotStart });
    }
}
