using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.InitialReportModule.Entities;
using PhysioAssist.Api.Modules.Intake.Entities;
using PhysioAssist.Api.Modules.PatientModule.Entities;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.SessionModule.Entities;
using PhysioAssist.Api.Shared.Entities;
using PhysioAssist.Api.Shared.Extensions;
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

    //Auth
    public DbSet<OtpEntry> OtpEntries { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // Patient
    public DbSet<Patient> Patients { get; set; }
    public DbSet<DoctorPatient> DoctorPatients { get; set; }

    // Intake
    public DbSet<PatientFormSchema> PatientFormSchemas { get; set; }
    public DbSet<PreVisitIntake> PreVisitIntakes { get; set; }

    // InitialReport
    public DbSet<InitialReport> InitialReports { get; set; }
    public DbSet<ReportAttachment> ReportAttachments { get; set; }

    // Session
    public DbSet<Session> Sessions { get; set; }
    public DbSet<SessionTranscription> SessionTranscriptions { get; set; }
    public DbSet<SessionTranscriptionChunk> SessionTranscriptionChunks { get; set; }

    // Scheduling
    public DbSet<ScheduleSlot> ScheduleSlots { get; set; }

    // Shared
    public DbSet<Notification> Notifications { get; set; }
}
