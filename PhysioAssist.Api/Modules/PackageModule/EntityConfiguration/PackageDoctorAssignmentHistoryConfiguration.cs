using PhysioAssist.Api.Modules.PackageModule.Entities;

namespace PhysioAssist.Api.Modules.PackageModule.EntityConfiguration;

public class PackageDoctorAssignmentHistoryConfiguration : IEntityTypeConfiguration<PackageDoctorAssignmentHistory>
{
    public void Configure(EntityTypeBuilder<PackageDoctorAssignmentHistory> builder)
    {
        builder.ToTable("PackageDoctorAssignmentHistories","package");

        builder.HasKey(h => h.UniqueId);

        builder.Property(h => h.Reason)
            .HasMaxLength(500);

        builder.Property(h => h.ChangeType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(h => h.PackageId);
        builder.HasIndex(h => new { h.PackageId, h.ChangedAt });
        builder.HasIndex(h => h.SessionId);
    }
}
