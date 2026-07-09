using PhysioAssist.Api.Modules.Intake.Entities;
using PhysioAssist.Api.Persistence;

namespace PhysioAssist.Api.Modules.Intake.Repositories;

public class PreVisitIntakeRepository(ApplicationDbContext context) : IPreVisitIntakeRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task AddAsync(PreVisitIntake intake, CancellationToken cancellationToken = default)
    {
        await _context.PreVisitIntakes.AddAsync(intake, cancellationToken);
    }

    public async Task<PreVisitIntake?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PreVisitIntakes
            .FirstOrDefaultAsync(intake => intake.Id == id, cancellationToken);
    }

    public async Task<PreVisitIntake?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PreVisitIntakes
            .Include(intake => intake.FormSchema)
            .FirstOrDefaultAsync(intake => intake.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PreVisitIntake>> GetByDoctorAsync(Guid doctorId, IntakeStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PreVisitIntakes
            .Where(intake => intake.DoctorId == doctorId);

        if (status is not null)
            query = query.Where(intake => intake.Status == status);

        return await query
            .OrderByDescending(intake => intake.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PreVisitIntake?> GetByAccessTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.PreVisitIntakes
            .FirstOrDefaultAsync(intake => intake.AccessTokenHash == tokenHash, cancellationToken);
    }

    public async Task<bool> ExistsConvertedAsync(Guid intakeId, CancellationToken cancellationToken = default)
    {
        return await _context.PreVisitIntakes
            .AnyAsync(intake => intake.Id == intakeId && intake.ConvertedToPatientId != null, cancellationToken);
    }

    public void Update(PreVisitIntake intake)
    {
        _context.PreVisitIntakes.Update(intake);
    }
}
