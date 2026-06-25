using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Reflection;

namespace PhysioAssist.Api.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : IdentityDbContext<ApplicationUser>(options)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();

        foreach (var entityEntery in entries)
        {
            var CurrentUserId = _httpContextAccessor.HttpContext!.User.GetUserId();

            if (entityEntery.State == EntityState.Added)
                entityEntery.Property(x => x.CreatedById).CurrentValue = CurrentUserId!;

            if (entityEntery.State == EntityState.Modified)
            {
                entityEntery.Property(x => x.UpdatedById).CurrentValue = CurrentUserId;
                entityEntery.Property(x => x.UpdatedAt).CurrentValue = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public DbSet<OtpEntry> OtpEntries { get; set; }
}
