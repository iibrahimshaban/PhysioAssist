using PhysioAssist.Api.Shared.Dtos.Doctor;

namespace PhysioAssist.Api.Shared.Interfaces.Exposed;

public interface IAuthQueryService
{
    Task<Result<DoctorResponse>> GetDoctorById(Guid id, CancellationToken cancellationToken = default);
}
