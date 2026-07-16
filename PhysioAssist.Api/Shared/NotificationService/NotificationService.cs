using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Entities;
using PhysioAssist.Api.Shared.Helpers;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.Interfaces.Common;

namespace PhysioAssist.Api.Shared.NotificationService;

// ⚠️ WhatsApp channel اتأجل مؤقتًا بناءً على طلبك - الخدمة دلوقتي بترسل Email بس،
// وبتسجل كل محاولة في جدول Notification. لما تحددي الـ WhatsApp provider (Twilio/n8n)،
// نضيف الـ Channel التاني بسهولة من غير ما نغيّر الـ interface.
public class NotificationService(
    ICustomEmailService emailService,
    ApplicationDbContext context) : INotificationService
{
    private readonly ICustomEmailService _emailService = emailService;
    private readonly ApplicationDbContext _context = context;

    public async Task SendReportReadyNotificationAsync(
        Guid doctorId,
        Guid patientId,
        string patientEmail,
        string patientName,
        string pdfUrl,
        string qrImageUrl)
    {
        var status = "Sent";

        try
        {
            var htmlBody = EmailBodyBuilder.ReportReady(patientName, pdfUrl, qrImageUrl);
            await _emailService.SendEmailAsync(patientEmail, "Your PhysioAssist Treatment Plan is Ready", htmlBody);
        }
        catch
        {
            status = "Failed";
        }

        var notification = new Notification
        {
            Id = Guid.CreateVersion7(),
            DoctorId = doctorId,
            PatientId = patientId,
            Channel = "Email",
            Type = "ReportReady",
            Status = status,
            SentAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }
}
