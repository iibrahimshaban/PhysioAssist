using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces
{
    public interface IWorkingScheduleService
    {

        Task<WorkingScheduleDto> CreateAsync(CreateWorkingScheduleRequest request, CancellationToken cancellationToken = default);
        Task<WorkingScheduleDto?> GetActiveByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
        Task<WorkingScheduleDto> UpdateDaysAsync(Guid workingScheduleId, UpdateWorkingScheduleDaysRequest request, CancellationToken cancellationToken = default);
        Task DeactivateAsync(Guid workingScheduleId, CancellationToken cancellationToken = default);
    }
}
