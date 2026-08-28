using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Web.Infrastructure.Email;

public class SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Email disabled. Would send to {Email}: {Subject}", toEmail, subject);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.User)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.User, _options.Password)
        };

        await client.SendMailAsync(message, ct);
    }
}

public class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("Email to {Email} | {Subject}\n{Body}", toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
