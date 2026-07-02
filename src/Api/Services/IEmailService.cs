namespace Oz.Api.Services;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(string to, string subject, string htmlBody);
}
