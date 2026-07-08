namespace PhysioAssist.Api.Shared.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Sends an email to the patient with the treatment plan PDF link and QR code,
    /// and logs the delivery attempt.
    /// </summary>
    Task SendReportReadyNotificationAsync(
        Guid doctorId,
        Guid patientId,
        string patientEmail,
        string patientName,
        string pdfUrl,
        string qrImageUrl);
}
