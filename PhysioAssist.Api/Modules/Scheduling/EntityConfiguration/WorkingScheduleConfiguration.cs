using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.EntityConfiguration;

public class WorkingScheduleConfiguration : IEntityTypeConfiguration<WorkingSchedule>
{
    public void Configure(EntityTypeBuilder<WorkingSchedule> builder)
    {
        builder.ToTable("WorkingSchedule", schema: "scheduling");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .ValueGeneratedNever();

        builder.Property(w => w.DoctorId)
            .IsRequired();

        // SlotDurationMinutes removed — duration is now per-appointment, not per-schedule

        builder.Property(w => w.IsActive)
            .IsRequired();

        builder.HasMany(w => w.Days)
            .WithOne(d => d.WorkingSchedule)
            .HasForeignKey(d => d.WorkingScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.DoctorId)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_WorkingSchedule_DoctorId_ActiveOnly");
    }
}