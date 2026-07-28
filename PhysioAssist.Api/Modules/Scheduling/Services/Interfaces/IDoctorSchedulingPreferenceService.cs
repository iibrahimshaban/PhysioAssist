using PhysioAssist.Api.Modules.Scheduling.DTO;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

public interface IDoctorSchedulingPreferenceService
{
    Task<Result<DoctorSchedulingPreferenceDto>> GetForDoctorAsync(Guid doctorId, CancellationToken ct = default);

    Task<Result<DoctorSchedulingPreferenceDto>> UpdateForDoctorAsync(Guid doctorId,UpdateDoctorSchedulingPreferenceRequest request,
        CancellationToken ct = default);
}
