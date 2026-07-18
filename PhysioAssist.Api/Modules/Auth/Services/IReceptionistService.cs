using PhysioAssist.Api.Modules.Auth.Contracts.Receptionist;

namespace PhysioAssist.Api.Modules.Auth.Services;

public interface IReceptionistService
{
    Task<Result<IEnumerable<ReceptionistResponse>>> GetAllAsync(Guid doctorId, CancellationToken cancellationToken=default);
    Task<Result<ReceptionistResponse>> GetByIdAsync(Guid receptionistId, CancellationToken cancellationToken = default);
    Task<Result<ReceptionistResponse>> CreateAsync(Guid doctorId, CreateReceptionistRequest request, CancellationToken cancellationToken = default);
    Task<Result<ReceptionistResponse>> UpdateAsync(Guid receptionistId, UpdateReceptionistRequest request, CancellationToken cancellationToken = default);
    Task<Result> ToggleDisabledAsync(Guid receptionistId, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid receptionistId, CancellationToken cancellationToken = default);
}
