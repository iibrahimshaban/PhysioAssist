using PhysioAssist.Api.Modules.SessionModule.Contracts;
using PhysioAssist.Api.Modules.SessionModule.Entities;
using PhysioAssist.Api.Modules.SessionModule.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Enums;

namespace PhysioAssist.Api.Modules.SessionModule.Services;

public class SessionService(ApplicationDbContext context) : ISessionService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result<SessionResponse>> CreateSessionAsync(CreateSessionRequest request)
    {

        var session = new Session
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            ScheduleSlotId = request.ScheduleSlotId
        };

        await _context.Sessions.AddAsync(session);


        await _context.SaveChangesAsync();

        var response = new SessionResponse
        {
            Id = session.Id,
            PatientId = session.PatientId,
            DoctorId = session.DoctorId,
            ScheduleSlotId = session.ScheduleSlotId,
            Summary = session.SummaryText,
            Status = session.Status
        };

        return Result.Success(response);
    }
    public async Task<Result> StartSessionAsync(Guid id)
    {
        var session = await _context.Sessions.FindAsync(id);

        if (session is null)
            return Result.Failure(SessionErrors.SessionNotFound);

        if (session.Status != SessionStatus.Scheduled)
            return Result.Failure(SessionErrors.InvalidSessionStatus);

        session.Status = SessionStatus.InProgress;

        await _context.SaveChangesAsync();

        return Result.Success();
    }
}