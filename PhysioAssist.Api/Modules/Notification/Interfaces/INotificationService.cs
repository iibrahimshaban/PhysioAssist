using PhysioAssist.Api.Modules.Notification.DTO;

namespace PhysioAssist.Api.Modules.Notification.Interfaces
{
    /// <summary>
    /// Public entry point for the Notification module. AppointmentService depends
    /// ONLY on this interface — it never knows or cares whether a notification
    /// is ultimately delivered by email, WhatsApp, SMS, or push.
    /// </summary>
    public interface INotificationService
    {
        Task NotifyAppointmentCreatedAsync(AppointmentNotificationDto appointment, CancellationToken cancellationToken = default);
        Task NotifyAppointmentCancelledAsync(AppointmentNotificationDto appointment, CancellationToken cancellationToken = default);
        Task NotifyAppointmentRescheduledAsync(AppointmentNotificationDto appointment, CancellationToken cancellationToken = default);
        Task NotifyAppointmentCompletedAsync(AppointmentNotificationDto appointment, CancellationToken cancellationToken = default);
        Task NotifyAppointmentNoShowAsync(AppointmentNotificationDto appointment, CancellationToken cancellationToken = default);
    }
}
