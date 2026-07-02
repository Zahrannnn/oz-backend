using Oz.Api.Services;

namespace Oz.Api.Jobs;

public class SendOrderCancelledEmailJob
{
    private readonly IEmailService _email;

    public SendOrderCancelledEmailJob(IEmailService email)
    {
        _email = email;
    }

    public async Task ExecuteAsync(long orderId, string customerEmail, string reason, string trackingUrl)
    {
        var html = $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>Order Cancelled</title></head>
        <body style="font-family:sans-serif;max-width:600px;margin:auto;padding:20px;">
            <h1>Order Cancelled</h1>
            <p>Your order <strong>#{orderId}</strong> has been cancelled.</p>
            <p>Reason: {reason}</p>
            <p>
                <a href="{trackingUrl}" style="display:inline-block;background:#2563eb;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;">
                    View Order
                </a>
            </p>
            <hr />
            <p style="color:#666;font-size:12px;">Oz School Uniforms</p>
        </body>
        </html>
        """;

        await _email.SendOrderConfirmationAsync(customerEmail, $"Order #{orderId} Cancelled", html);
    }
}
