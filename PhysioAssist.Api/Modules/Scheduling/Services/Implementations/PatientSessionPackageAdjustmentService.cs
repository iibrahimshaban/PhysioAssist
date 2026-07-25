using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class PatientSessionPackageAdjustmentService(ApplicationDbContext context) : IPatientSessionPackageAdjustmentService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result> ExtendByOneSessionAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await _context.Set<PatientSessionPackage>()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result.Failure(SchedulingErrors.PackageNotFound);

        package.TotalSessions++;
        package.RemainingSessions++;

        if (package.Status == PackageStatus.Completed)
            package.Status = PackageStatus.Active;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
