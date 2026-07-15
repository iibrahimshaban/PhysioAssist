using PhysioAssist.Api.Modules.Notification.Interfaces;

namespace PhysioAssist.Api.Modules.Notification.Services
{
    /// <summary>
    /// Adapts the generic Shared EmailService to the module's INotificationChannel
    /// contract. This is the ONLY class in the Notification module that knows
    /// SMTP/email exists — NotificationService itself is channel-agnostic.
    /// </summary>
    public class EmailNotificationChannel(ICustomEmailService emailService) : INotificationChannel
    {
        private readonly ICustomEmailService _emailService = emailService;

        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
        {
            // Shared EmailService predates CancellationToken support and has no
            // parameter for one — nothing to pass through here.
            return _emailService.SendEmailAsync(recipient, subject, body);
        }
    }
}