using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PhysioAssist.Api.Shared.Interfaces.Common;

namespace PhysioAssist.Api.Shared.Email;

public class EmailService(IOptions<MailSettings> options) : ICustomEmailService
{
    private readonly MailSettings _mailSettings = options.Value;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage) =>
        await SendAsync(email, subject, htmlMessage, null, null, null);

    public async Task SendEmailWithAttachmentAsync(
        string email, string subject, string htmlMessage,
        byte[] attachmentBytes, string attachmentFileName, string attachmentContentType = "application/pdf") =>
        await SendAsync(email, subject, htmlMessage, attachmentBytes, attachmentFileName, attachmentContentType);

    private async Task SendAsync(
        string email, string subject, string htmlMessage,
        byte[]? attachmentBytes, string? attachmentFileName, string? attachmentContentType)
    {
        var message = new MimeMessage();
        message.Sender = new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail);
        message.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };

        if (attachmentBytes is not null && attachmentFileName is not null)
            bodyBuilder.Attachments.Add(attachmentFileName, attachmentBytes, ContentType.Parse(attachmentContentType ?? "application/octet-stream"));

        message.Body = bodyBuilder.ToMessageBody();

        using var smtp = new SmtpClient();
        smtp.CheckCertificateRevocation = false;

        await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_mailSettings.Mail, _mailSettings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
