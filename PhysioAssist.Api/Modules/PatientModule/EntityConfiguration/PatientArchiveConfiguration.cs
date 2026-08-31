using PhysioAssist.Api.Modules.PatientModule.Entities;

namespace PhysioAssist.Api.Modules.PatientModule.EntityConfiguration;

public class PatientArchiveConfiguration : IEntityTypeConfiguration<PatientArchive>
{
    public void Configure(EntityTypeBuilder<PatientArchive> builder)
    {
        builder.ToTable("PatientArchives", "Patient");
    }
}
