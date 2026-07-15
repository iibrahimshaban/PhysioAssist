namespace PhysioAssist.Api.Modules.Notification.Interfaces
{
    /// <summary>
    /// A single delivery mechanism (Email today; WhatsApp/SMS/Push in V2).
    /// NotificationService depends on this abstraction, not on any concrete
    /// transport — adding a new channel means implementing this interface and
    /// registering it in DI, never touching NotificationService's logic for
    /// WHO to notify or WHICH template to use.
    /// </summary>
    public interface INotificationChannel
    {
        Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
    }
}
