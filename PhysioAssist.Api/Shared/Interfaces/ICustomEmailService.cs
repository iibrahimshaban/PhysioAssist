namespace PhysioAssist.Api.Shared.Interfaces;

public interface ICustomEmailService
{
    Task SendEmailAsync(string Email, string subject, string htmlMessage);
}
