using PhysioAssist.Api.Modules.InitialReportModule.DTOs;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IInitialReportQueryService
{
    Task<List<InitialReportResponse>> GetPatientReportsAsync(Guid patientId);
    Task<InitialReportResponse?> GetReportWithAttachmentsAsync(Guid reportId);
}
