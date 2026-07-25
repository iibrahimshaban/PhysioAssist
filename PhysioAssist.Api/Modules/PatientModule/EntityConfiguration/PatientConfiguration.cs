using PhysioAssist.Api.Modules.PatientModule.Entities;

namespace PhysioAssist.Api.Modules.PatientModule.EntityConfiguration;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.ToTable("Patient", schema: "patient");

        builder.Property(p => p.FullName)
               .HasMaxLength(100);

        builder
            .Property(p => p.DateOfBirth).IsRequired(false);

        builder.Property(p => p.Gender)
               .HasMaxLength(10);

        builder.Property(p => p.PhoneNumber)
               .HasMaxLength(20);

        builder.Property(p => p.EmailAddress)
               .HasMaxLength(200);

        builder.Property(p => p.QRCodeToken)
               .HasMaxLength(500);

        builder.Property(p => p.Status)
               .HasConversion<int>();

        builder.Property(p => p.ParsedPreferredWeekdays)
               .HasConversion<int>();

        builder.HasIndex(p => p.QRCodeToken)
               .IsUnique();

        builder.HasIndex(p => p.EmailAddress)
               .IsUnique();
    }
}
