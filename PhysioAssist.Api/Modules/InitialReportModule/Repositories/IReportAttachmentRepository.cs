using PhysioAssist.Api.Modules.InitialReportModule.Entities;
using PhysioAssist.Api.Shared.Interfaces.Common;

namespace PhysioAssist.Api.Modules.InitialReportModule.Repositories;

public interface IReportAttachmentRepository : IBaseRepository<ReportAttachment>
{
    Task<ReportAttachment?> GetByIdAsync(Guid id);
}
