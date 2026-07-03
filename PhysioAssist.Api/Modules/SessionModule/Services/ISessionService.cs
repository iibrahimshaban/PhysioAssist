using PhysioAssist.Api.Modules.SessionModule.Contracts;

namespace PhysioAssist.Api.Modules.SessionModule.Services
{
    public interface ISessionService
    {
         Task<Result<SessionResponse>> CreateSessionAsync(CreateSessionRequest request);
        Task<Result> StartSessionAsync(Guid id);
    }
}
