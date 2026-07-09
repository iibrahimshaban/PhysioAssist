using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.EntityConfiguration;

public class WorkingScheduleDayConfiguration : IEntityTypeConfiguration<WorkingScheduleDay>
{
    public void Configure(EntityTypeBuilder<WorkingScheduleDay> builder)
    {
        builder.ToTable("WorkingScheduleDay", schema: "scheduling");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.WorkingScheduleId)
            .IsRequired();

        builder.Property(d => d.Day)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.StartTime)
            .IsRequired();

        builder.Property(d => d.EndTime)
            .IsRequired();

        // Structurally prevents duplicate/overlapping day rows —
        // one row per (schedule, weekday) makes overlap impossible, per point 6
        builder.HasIndex(d => new { d.WorkingScheduleId, d.Day })
            .IsUnique()
            .HasDatabaseName("IX_WorkingScheduleDay_Schedule_Day_Unique");
    }
}