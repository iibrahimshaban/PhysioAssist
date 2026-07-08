using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Shared.ResultPattern;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces
{
    public interface IAppointmentValidator
    {
        Task<Result> ValidateCreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);

        Task<Result> ValidateRescheduleAsync(ScheduleSlot existing, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default);

        Result ValidateCancel(ScheduleSlot existing);

        Result ValidateComplete(ScheduleSlot existing);

        Result ValidateNoShow(ScheduleSlot existing);
    }
}