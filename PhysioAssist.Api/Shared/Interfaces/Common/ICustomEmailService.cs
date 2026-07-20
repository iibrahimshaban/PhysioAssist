namespace PhysioAssist.Api.Shared.Interfaces.Common;

public interface ICustomEmailService
{
    Task SendEmailAsync(string Email, string subject, string htmlMessage);
    Task SendEmailWithAttachmentAsync(
        string email, string subject, string htmlMessage,
        byte[] attachmentBytes, string attachmentFileName, string attachmentContentType = "application/pdf");
}
