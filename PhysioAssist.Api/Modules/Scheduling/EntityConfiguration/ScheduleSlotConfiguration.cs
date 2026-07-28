using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.EntityConfiguration;

public class ScheduleSlotConfiguration : IEntityTypeConfiguration<ScheduleSlot>
{
    public void Configure(EntityTypeBuilder<ScheduleSlot> builder)
    {
        builder.ToTable("ScheduleSlot", schema: "scheduling", t =>
        {
            t.HasCheckConstraint(
                "CK_ScheduleSlot_ExactlyOneOwner",
                "([PatientId] IS NOT NULL AND [GuestId] IS NULL) OR ([PatientId] IS NULL AND [GuestId] IS NOT NULL)");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.DoctorId)
            .IsRequired();

        // PatientId / GuestId: intentionally left with no explicit .IsRequired() —
        // both nullable by convention now (Guid? maps to a nullable column).
        // The XOR rule itself is enforced twice: at the DB level via the check
        // constraint above, and at the application level in AppointmentValidator
        // (so callers get a clean 400 with a Result error instead of a raw SQL
        // exception).

        builder.Property(s => s.SlotStart)
            .IsRequired();

        builder.Property(s => s.SlotEnd)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(s => new { s.DoctorId, s.SlotStart, s.SlotEnd })
            .HasDatabaseName("IX_ScheduleSlot_DoctorId_SlotStart_SlotEnd");

        builder.HasOne(s => s.Package)
            .WithMany()
            .HasForeignKey(s => s.PackageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Guest)
            .WithMany()
            .HasForeignKey(s => s.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}