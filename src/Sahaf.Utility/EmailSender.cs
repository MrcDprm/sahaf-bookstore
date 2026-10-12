using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sahaf.Utility;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}

public class EmailSettings
{
    public string? ResendApiKey { get; set; }
    public string From { get; set; } = "Sahaf <no-reply@miracdeprem.com>";
}

// Sends mail through the Resend HTTP API. Without an API key (local
// development) the message is written to the log instead, so the
// confirmation link can still be opened.
public class ResendEmailSender(HttpClient http, IOptions<EmailSettings> options, ILogger<ResendEmailSender> logger)
    : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ResendApiKey))
        {
            logger.LogWarning("Resend API key is not set. E-mail to {Email}: {Subject}\n{Body}", email, subject, htmlMessage);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(new { from = settings.From, to = new[] { email }, subject, html = htmlMessage }),
        };
        request.Headers.Authorization = new("Bearer", settings.ResendApiKey);

        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            // Log only the status: the response body can echo the address.
            logger.LogError("Resend returned {Status} for subject {Subject}", (int)response.StatusCode, subject);
            throw new InvalidOperationException("E-mail could not be sent.");
        }
    }
}
