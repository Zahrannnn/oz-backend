using System.Net;
using System.Net.Mail;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Services;

public class SmtpEmailService
{
    private readonly string? _host;
    private readonly int _port;
    private readonly string? _user;
    private readonly string? _pass;
    private readonly string? _from;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IConfiguration config,
        IServiceScopeFactory scopeFactory,
        ILogger<SmtpEmailService> logger)
    {
        _host = config["Email:SmtpHost"] ?? Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST");
        _port = int.TryParse(config["Email:SmtpPort"] ?? Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT"), out var p) ? p : 587;
        _user = config["Email:Username"] ?? Environment.GetEnvironmentVariable("EMAIL_USERNAME");
        _pass = config["Email:Password"] ?? Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
        _from = config["Email:From"] ?? Environment.GetEnvironmentVariable("EMAIL_FROM") ?? "noreply@ozuniforms.com";
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_host) || string.IsNullOrEmpty(_user) || string.IsNullOrEmpty(_pass))
        {
            _logger.LogInformation("[Email] SMTP not configured. Would send to {To}: {Subject}", to, subject);
            await LogSendAsync(to, subject, null, null);
            return;
        }

        try
        {
            using var client = new SmtpClient(_host, _port)
            {
                Credentials = new NetworkCredential(_user, _pass),
                EnableSsl = true
            };

            using var msg = new MailMessage(_from!, to, subject, htmlBody) { IsBodyHtml = true };
            await client.SendMailAsync(msg);

            _logger.LogInformation("[Email] Sent to {To}: {Subject}", to, subject);
            await LogSendAsync(to, subject, null, EmailStatus.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send to {To}", to);
            await LogSendAsync(to, subject, ex.Message, EmailStatus.Failed);
            throw;
        }
    }

    private async Task LogSendAsync(string recipient, string template, string? error, EmailStatus? status = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.EmailLogs.Add(new EmailLog
        {
            Recipient = recipient,
            Template = template,
            Status = status ?? EmailStatus.Pending,
            Error = error,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
