namespace Oz.Api.Services;

public class ConsoleEmailService : IEmailService
{
    public Task SendOrderConfirmationAsync(string to, string subject, string htmlBody)
    {
        Console.WriteLine($"[Email (placeholder)] To: {to}, Subject: {subject}");
        Console.WriteLine($"  Body length: {htmlBody.Length} chars");
        return Task.CompletedTask;
    }
}
