using Oz.Api.Services;

namespace Oz.Api.Jobs;

public class SendOrderDeliveredEmailJob
{
    private readonly IEmailService _email;

    public SendOrderDeliveredEmailJob(IEmailService email)
    {
        _email = email;
    }

    public async Task ExecuteAsync(long orderId, string customerEmail, string trackingUrl)
    {
        var html = $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>Order Delivered</title></head>
        <body style="font-family:sans-serif;max-width:600px;margin:auto;padding:20px;">
            <h1>Order Delivered!</h1>
            <p>Your order <strong>#{orderId}</strong> has been delivered.</p>
            <p>Thank you for shopping with us!</p>
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

        await _email.SendOrderConfirmationAsync(customerEmail, $"Order #{orderId} Delivered", html);
    }
}
