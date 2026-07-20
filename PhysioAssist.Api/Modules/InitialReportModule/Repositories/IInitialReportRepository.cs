using PhysioAssist.Api.Modules.InitialReportModule.Entities;
using PhysioAssist.Api.Shared.Interfaces.Common;

namespace PhysioAssist.Api.Modules.InitialReportModule.Repositories;

public interface IInitialReportRepository : IBaseRepository<InitialReport>
{
    Task<InitialReport?> GetByIdAsync(Guid id);
    Task<InitialReport?> GetWithAttachmentsAsync(Guid id);
    Task<List<InitialReport>> GetByPatientIdAsync(Guid patientId);
    Task<InitialReport?> GetReportByPatientIdAsync(Guid patientId);
}
