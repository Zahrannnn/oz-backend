using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Oz.Api.Services;

namespace Oz.Api.Jobs;

public class SendEmailJob
{
    private readonly SmtpEmailService _email;
    private readonly ILogger<SendEmailJob> _logger;

    public SendEmailJob(SmtpEmailService email, ILogger<SendEmailJob> logger)
    {
        _email = email;
        _logger = logger;
    }

    public async Task ExecuteAsync(PerformContext context, string to, string subject, string html)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["job_id"] = context.BackgroundJob.Id,
                   ["job_type"] = nameof(SendEmailJob)
               }))
        {
            _logger.LogInformation("Sending email to {Recipient} with subject {Subject}", to, subject);
            await _email.SendAsync(to, subject, html);
            _logger.LogInformation("Email sent to {Recipient} with subject {Subject}", to, subject);
        }
    }
}
