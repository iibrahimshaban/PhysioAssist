using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Shared.Email;

public class EmailService(IOptions<MailSettings> options) : ICustomEmailService
{
    private readonly MailSettings _mailSettings = options.Value;

    public async Task SendEmailAsync(string Email, string subject, string htmlMessage)
    {
        var message = new MimeMessage();

        message.Sender = new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail);
        message.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail));
        message.To.Add(MailboxAddress.Parse(Email));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlMessage }.ToMessageBody();

        using var smtp = new SmtpClient();
        smtp.CheckCertificateRevocation = false;

        await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_mailSettings.Mail, _mailSettings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
