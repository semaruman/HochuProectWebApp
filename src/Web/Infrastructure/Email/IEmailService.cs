namespace Web.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@hochuproect.local";
    public string FromName { get; set; } = "Хочу Проект";
}

public class AppOptions
{
    public const string SectionName = "App";
    public string PublicBaseUrl { get; set; } = "http://localhost:5121";
}

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
