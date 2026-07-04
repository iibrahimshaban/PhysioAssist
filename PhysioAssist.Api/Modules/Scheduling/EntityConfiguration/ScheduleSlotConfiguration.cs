using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.EntityConfiguration;

public class ScheduleSlotConfiguration : IEntityTypeConfiguration<ScheduleSlot>
{
    public void Configure(EntityTypeBuilder<ScheduleSlot> builder)
    {
        builder.ToTable("ScheduleSlot", schema: "scheduling");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.DoctorId)
            .IsRequired();

        // No longer nullable — a ScheduleSlot only exists once it's an actual booked appointment
        builder.Property(s => s.PatientId)
            .IsRequired();

        builder.Property(s => s.SlotStart)
            .IsRequired();

        builder.Property(s => s.SlotEnd)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<int>()
            .IsRequired();

        // WorkingScheduleId FK removed entirely — no generation source to trace anymore

        // Non-unique: overlap can't be expressed as column equality (different SlotStart values
        // can still overlap), so this index exists purely to make the per-doctor/per-day
        // availability-calculation query fast, not to enforce a constraint.
        // Actual double-booking protection happens in the application layer via sp_getapplock.
        builder.HasIndex(s => new { s.DoctorId, s.SlotStart, s.SlotEnd })
            .HasDatabaseName("IX_ScheduleSlot_DoctorId_SlotStart_SlotEnd");
    }
}