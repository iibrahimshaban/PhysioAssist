using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces
{
    public interface IAppointmentValidator
    {

        Task ValidateCreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);

        Task ValidateRescheduleAsync(ScheduleSlot existing, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default);

        void ValidateCancel(ScheduleSlot existing);

        void ValidateComplete(ScheduleSlot existing);

        void ValidateNoShow(ScheduleSlot existing);
    }
}
