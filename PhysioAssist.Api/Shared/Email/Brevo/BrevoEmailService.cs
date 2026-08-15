using Microsoft.Extensions.Options;

namespace PhysioAssist.Api.Shared.Email.Brevo;

public class BrevoEmailService : ICustomEmailService
{
    private readonly BrevoOptions _options;
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly HttpClient _httpClient;

    public BrevoEmailService(IOptions<BrevoOptions> options, ILogger<BrevoEmailService> logger, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri("https://api.brevo.com/v3/");
        _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
    }

    public async Task SendEmailAsync(string Email, string subject, string htmlMessage)
    {
        await SendAsync(Email, subject, htmlMessage, null, null, null);
    }

    public async Task SendEmailWithAttachmentAsync(
        string email, string subject, string htmlMessage,
        byte[] attachmentBytes, string attachmentFileName, string attachmentContentType = "application/pdf")
    {
        await SendAsync(email, subject, htmlMessage, attachmentBytes, attachmentFileName, attachmentContentType);
    }

    private async Task SendAsync(string email, string subject, string htmlMessage,
        byte[]? attachmentBytes, string? attachmentFileName, string? attachmentContentType)
    {
        var payload = new
        {
            sender = new { name = _options.FromName, email = _options.FromEmail },
            to = new[] { new { email } },
            subject,
            htmlContent = htmlMessage,
            attachment = attachmentBytes != null
                ? new[] { new { content = Convert.ToBase64String(attachmentBytes), name = attachmentFileName } }
                : null
        };

        var response = await _httpClient.PostAsJsonAsync("smtp/email", payload);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Brevo failed sending to {Email}: {Status} {Body}", email, response.StatusCode, body);
        }
    }
}
