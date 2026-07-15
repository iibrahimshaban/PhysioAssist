using PhysioAssist.Api.Modules.Notification.DTO;
using PhysioAssist.Api.Modules.Notification.Interfaces;

namespace PhysioAssist.Api.Modules.Notification.Services
{

    /// <summary>
    /// Decides WHO gets notified, WHICH template applies, and WHICH channel
    /// delivers it. Contains no SMTP/transport code and no HTML — those live in
    /// INotificationChannel and IEmailTemplateService respectively.
    /// </summary>
    /// <remarks>
    /// Delivery failures are logged and swallowed, never thrown back to the
    /// caller. Per the architecture requirement: an appointment is already
    /// committed to the database by the time this runs, and a failed email
    /// must never roll back or fail that already-successful business operation.
    /// </remarks>
    public class NotificationService(
        IEmailTemplateService templateService,
        INotificationChannel emailChannel,
        ILogger<NotificationService> logger)
        : INotificationService
    {
        private readonly IEmailTemplateService _templateService = templateService;
        private readonly INotificationChannel _emailChannel = emailChannel;
        private readonly ILogger<NotificationService> _logger = logger;

        public Task NotifyAppointmentCreatedAsync(AppointmentNotificationDto appointment, CancellationToken cancellationToken = default) =>
            SendSafelyAsync(appointment, _templateService.BuildAppointmentCreated, nameof(NotifyAppointmentCreatedAsync), cancellationToken);

        public Task NotifyAppointmentCancelledAsync(AppointmentNotificationDto appointment, CancellationToken cancellationToken = default) =>
            SendSafelyAsync(appointment, _templateService.BuildAppointmentCancelled, nameof(NotifyAppointmentCancelledAsync), cancellationToken);

        public Task NotifyAppointmentRescheduledAsync(AppointmentNotificationDto appointment, CancellationToken cancellationToken = default) =>
            SendSafelyAsync(appointment, _templateService.BuildAppointmentRescheduled, nameof(NotifyAppointmentRescheduledAsync), cancellationToken);

        public Task NotifyAppointmentCompletedAsync(AppointmentNotificationDto appointment, CancellationToken cancellationToken = default) =>
            SendSafelyAsync(appointment, _templateService.BuildAppointmentCompleted, nameof(NotifyAppointmentCompletedAsync), cancellationToken);

        public Task NotifyAppointmentNoShowAsync(AppointmentNotificationDto appointment, CancellationToken cancellationToken = default) =>
            SendSafelyAsync(appointment, _templateService.BuildAppointmentNoShow, nameof(NotifyAppointmentNoShowAsync), cancellationToken);

        private async Task SendSafelyAsync(
            AppointmentNotificationDto appointment,
            Func<AppointmentNotificationDto, (string Subject, string HtmlBody)> buildTemplate,
            string eventName,
            CancellationToken cancellationToken)
        {
            try
            {
                var (subject, htmlBody) = buildTemplate(appointment);
                await _emailChannel.SendAsync(appointment.PatientEmail, subject, htmlBody, cancellationToken);
            }
            catch (Exception ex)
            {
                // Swallowed by design — see class remarks. Logged with enough
                // context to diagnose delivery issues without exposing PII in
                // the log message itself beyond what's already routine (email).
                _logger.LogError(ex, "Failed to send {EventName} notification to {Recipient}", eventName, appointment.PatientEmail);
            }
        }
    }
}
