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

        builder.Property(w => w.ClinicId)
            .IsRequired();

        // SlotDurationMinutes removed — duration is now per-appointment, not per-schedule

        builder.Property(w => w.IsActive)
            .IsRequired();

        builder.HasMany(w => w.Days)
            .WithOne(d => d.WorkingSchedule)
            .HasForeignKey(d => d.WorkingScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        // One active DEFAULT schedule per clinic (DoctorId IS NULL)
        builder.HasIndex(w => w.ClinicId)
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [DoctorId] IS NULL")
            .HasDatabaseName("IX_WorkingSchedule_ClinicId_ActiveDefaultOnly");

        // One active OVERRIDE schedule per doctor (DoctorId IS NOT NULL)
        builder.HasIndex(w => w.DoctorId)
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [DoctorId] IS NOT NULL")
            .HasDatabaseName("IX_WorkingSchedule_DoctorId_ActiveOverrideOnly");
    }
}