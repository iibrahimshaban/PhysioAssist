using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.EntityConfiguration
{
    public class GuestConfiguration : IEntityTypeConfiguration<Guest>
    {
        public void Configure(EntityTypeBuilder<Guest> builder)
        {
            builder.ToTable("Guest", schema: "scheduling");

            builder.HasKey(g => g.Id);

            builder.Property(g => g.Id)
                .ValueGeneratedNever();

            builder.Property(g => g.FullName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(g => g.PhoneNumber)
                .IsRequired()
                .HasMaxLength(30);
        }
    }
}
