using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Shared.ResultPattern;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces
{
    public interface IWorkingScheduleService
    {
        Task<Result<WorkingScheduleDto>> CreateAsync(CreateWorkingScheduleRequest request, CancellationToken cancellationToken = default);

        Task<Result<WorkingScheduleDto>> GetActiveByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);

        Task<Result<WorkingScheduleDto>> UpdateDaysAsync(Guid workingScheduleId, UpdateWorkingScheduleDaysRequest request, CancellationToken cancellationToken = default);

        Task<Result> DeactivateAsync(Guid workingScheduleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes a working schedule (and its days) from the system.
        /// Unlike <see cref="DeactivateAsync"/>, this is not reversible and does not
        /// preserve history — use Deactivate for the normal "stop using this schedule" flow.
        /// </summary>
        Task<Result> DeleteAsync(Guid workingScheduleId, CancellationToken cancellationToken = default);
    }
}