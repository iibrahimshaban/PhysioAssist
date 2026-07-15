using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.DocumentationModule.Entities;
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
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();

        foreach (var entityEntry in entries)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User.GetUserId();

            if (entityEntry.State == EntityState.Added)
                entityEntry.Entity.CreatedById = currentUserId ?? entityEntry.Entity.CreatedById;

            if (entityEntry.State == EntityState.Modified)
            {
                entityEntry.Property(x => x.UpdatedById).CurrentValue = currentUserId;
                entityEntry.Property(x => x.UpdatedAt).CurrentValue = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
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
    public DbSet<SessionAttachment> SessionAttachments { get; set; }

    // Scheduling
    public DbSet<ScheduleSlot> ScheduleSlots { get; set; }
    public DbSet<WorkingSchedule> workingSchedules { get; set; }
    public DbSet<WorkingScheduleDay> workingScheduleDays { get; set; }
    //documentation 
    public DbSet<DocumentationSummary> DocumentationSummaries { get; set; }
    public DbSet<DocumentationTemplate> DocumentationTemplates { get; set; }
    public DbSet<DoctorDocumentationPreference> DoctorDocumentationPreferences { get; set; }
    public DbSet<SessionProgressNote> SessionProgressNotes { get; set; }

    // Shared
    public DbSet<Notification> Notifications { get; set; }
}
