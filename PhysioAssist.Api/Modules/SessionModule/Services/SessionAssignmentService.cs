using PhysioAssist.Api.Modules.SessionModule.Entities;
using PhysioAssist.Api.Modules.SessionModule.Errors;

namespace PhysioAssist.Api.Modules.SessionModule.Services;

public class SessionAssignmentService(ApplicationDbContext _context) : ISessionAssignmentService
{
    public async Task<Result> UpdateSessionDoctorAsync(
    Guid sessionId, Guid newDoctorId, CancellationToken cancellationToken = default)
    {
        var session = await _context.Set<Session>()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SessionErrors.SessionNotFound);  

        session.DoctorId = newDoctorId;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
