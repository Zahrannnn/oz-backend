using Oz.Api.Services;

namespace Oz.Api.Jobs;

public class SendEmailJob
{
    private readonly SmtpEmailService _email;

    public SendEmailJob(SmtpEmailService email)
    {
        _email = email;
    }

    public async Task ExecuteAsync(string to, string subject, string html)
    {
        await _email.SendAsync(to, subject, html);
    }
}
