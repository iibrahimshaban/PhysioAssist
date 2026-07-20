namespace PhysioAssist.Api.Shared.Interfaces;

public interface INotificationService
{
    Task SendReportReadyNotificationAsync(Guid doctorId, Guid patientId, string patientEmail, string patientName, string pdfUrl);
    Task SendTreatmentPlanEmailAsync(string patientEmail, string patientName, string pdfUrl);

}
