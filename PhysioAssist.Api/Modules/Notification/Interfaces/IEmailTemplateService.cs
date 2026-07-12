using PhysioAssist.Api.Modules.Notification.DTO;

namespace PhysioAssist.Api.Modules.Notification.Interfaces
{
    /// <summary>
    /// Produces the subject line and HTML body for each appointment event.
    /// Pure content generation — no delivery, no I/O.
    /// </summary>
    public interface IEmailTemplateService
    {
        (string Subject, string HtmlBody) BuildAppointmentCreated(AppointmentNotificationDto appointment);
        (string Subject, string HtmlBody) BuildAppointmentCancelled(AppointmentNotificationDto appointment);
        (string Subject, string HtmlBody) BuildAppointmentRescheduled(AppointmentNotificationDto appointment);
        (string Subject, string HtmlBody) BuildAppointmentCompleted(AppointmentNotificationDto appointment);
        (string Subject, string HtmlBody) BuildAppointmentNoShow(AppointmentNotificationDto appointment);
    }
}
