using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
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

// Sends mail through the Resend HTTP API. Without an API key the message is
// not sent; in development it is written to the log so the confirmation link
// can still be opened. Production logs never contain the links.
public class ResendEmailSender(
    HttpClient http,
    IOptions<EmailSettings> options,
    IHostEnvironment environment,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ResendApiKey))
        {
            if (environment.IsDevelopment())
                logger.LogWarning("Resend API key is not set. E-mail to {Email}: {Subject}\n{Body}", email, subject, htmlMessage);
            else
                logger.LogError("Resend API key is not set; an e-mail was not sent ({Subject}).", subject);
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
