using PhysioAssist.Api.Modules.Scheduling.DTO;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces
{
    public interface IWorkingScheduleService
    {
        Task<Result<WorkingScheduleDto>> CreateClinicDefaultAsync(
        Guid clinicId, CreateWorkingScheduleRequest request, CancellationToken cancellationToken = default);
        Task<Result<WorkingScheduleDto>> CreateDoctorOverrideAsync(
        Guid clinicId, Guid doctorId, CreateWorkingScheduleRequest request, CancellationToken cancellationToken = default);
        Task<Result<WorkingScheduleDto>> GetEffectiveByDoctorAsync(
        Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
        Task<Result<WorkingScheduleDto>> UpdateDaysAsync(Guid workingScheduleId, UpdateWorkingScheduleDaysRequest request, CancellationToken cancellationToken = default);

        Task<Result> DeactivateAsync(Guid workingScheduleId, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(Guid workingScheduleId, CancellationToken cancellationToken = default);
    }
}