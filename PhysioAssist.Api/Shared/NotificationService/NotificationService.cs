using Hangfire;
using PhysioAssist.Api.Shared.Helpers;
using PhysioAssist.Api.Shared.Interfaces.Common;


namespace PhysioAssist.Api.Shared.NotificationService;

public class NotificationService(ICustomEmailService _emailService,IHttpClientFactory httpClientFactory) : INotificationService
{
    public Task SendReportReadyNotificationAsync(Guid doctorId, Guid patientId, string patientEmail, string patientName, string pdfUrl)
    {
        BackgroundJob.Enqueue(() => SendTreatmentPlanEmailAsync(patientEmail, patientName, pdfUrl));
        return Task.CompletedTask;
    }

    public async Task SendTreatmentPlanEmailAsync(string patientEmail, string patientName, string pdfUrl)
    {
        var httpClient = httpClientFactory.CreateClient();
        var pdfBytes = await httpClient.GetByteArrayAsync(pdfUrl);
        var html = EmailBodyBuilder.ReportReady(patientName, pdfUrl);
        await _emailService.SendEmailWithAttachmentAsync(patientEmail, "Your PhysioAssist Treatment Plan", html, pdfBytes, "treatment-plan.pdf");
    }
}
