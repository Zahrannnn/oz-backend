using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.Jobs;
using Oz.Api.Services;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Webhooks;

[ApiController]
[Tags("Webhooks")]
[Route("api/v1/webhooks/bosta")]
public class BostaWebhookController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;
    private readonly IBackgroundJobClient _jobs;

    public BostaWebhookController(AppDbContext db, AuditLogService auditLog, IBackgroundJobClient jobs)
    {
        _db = db;
        _auditLog = auditLog;
        _jobs = jobs;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var signatureHeader = Request.Headers["X-Bosta-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signatureHeader))
            return Unauthorized();

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync();

        var secret = Environment.GetEnvironmentVariable("BOSTA_WEBHOOK_SECRET");
        if (string.IsNullOrEmpty(secret))
            return StatusCode(500, new { error = "webhook_secret_not_configured" });

        var computedHash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(rawBody));
        var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature),
                Encoding.UTF8.GetBytes(signatureHeader.ToLowerInvariant())))
            return Unauthorized();

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var trackingId = root.GetProperty("trackingId").GetString();
        var status = root.GetProperty("status").GetString();

        if (string.IsNullOrEmpty(trackingId) || string.IsNullOrEmpty(status))
            return BadRequest(new { error = "missing_tracking_id_or_status" });

        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(o => o.BostaTrackingId == trackingId);

        if (order == null)
            return NotFound(new { error = "order_not_found", trackingId });

        var now = DateTime.UtcNow;
        var trackingUrl = $"https://bosta.co/tracking/{trackingId}";

        switch (status)
        {
            case "in_transit":
                if (order.State >= OrderState.InTransit)
                    return Ok(new { status = "ok" });

                if (order.State != OrderState.HandedToCourier)
                    return Ok(new { status = "ok", note = "unexpected_state" });

                order.State = OrderState.InTransit;
                order.StateChangedAt = now;
                order.InTransitAt = now;
                await _db.SaveChangesAsync();
                await _auditLog.WriteAsync(Guid.Empty, "order.webhook.in_transit", "order", order.Id.ToString());
                break;

            case "delivered":
                if (order.State >= OrderState.Delivered)
                    return Ok(new { status = "ok" });

                if (order.State != OrderState.InTransit)
                    return Ok(new { status = "ok", note = "unexpected_state" });

                order.State = OrderState.ClosedSuccess;
                order.StateChangedAt = now;
                order.DeliveredAt = now;
                await _db.SaveChangesAsync();
                await _auditLog.WriteAsync(Guid.Empty, "order.webhook.delivered", "order", order.Id.ToString());

                var deliveredHtml = $"""
                <!DOCTYPE html>
                <html lang="ar" dir="rtl">
                <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width,initial-scale=1">
                <title>وصل الطلب</title>
                <link rel="preconnect" href="https://fonts.googleapis.com">
                <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@700;800&family=Work+Sans:wght@400;600&display=swap" rel="stylesheet">
                </head>
                <body style="margin:0;padding:24px 16px;background-color:#fff8f0;font-family:'Work Sans',sans-serif;direction:rtl;text-align:right;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;width:100%;margin:0 auto;background-color:#ffffff;border:3px solid #1f1b10;border-radius:16px;box-shadow:6px 6px 0 #1f1b10;">
                <tr>
                <td style="padding:32px 24px 0;">
                <p style="font-family:'Plus Jakarta Sans',sans-serif;font-size:12px;font-weight:700;color:#725c00;margin:0 0 8px;">&#127881; تم التوصيل</p>
                <h1 style="font-family:'Plus Jakarta Sans',sans-serif;font-size:32px;font-weight:800;line-height:40px;letter-spacing:-0.01em;color:#1f1b10;margin:0 0 16px;">طلبك وصل!</h1>
                <p style="font-size:16px;line-height:24px;color:#4d4632;margin:0 0 24px;">طلبك <strong>#{order.OrderNumber}</strong> اتعمل توصيل. شكراً إنك تسوقت معانا!</p>
                </td>
                </tr>
                <tr>
                <td style="padding:0 24px 32px;text-align:center;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;">
                <tr>
                <td style="border-radius:8px;border:3px solid #1f1b10;box-shadow:6px 6px 0 #1f1b10;background-color:#ffd200;text-align:center;padding:0;">
                <a href="{trackingUrl}" style="display:block;padding:14px 24px;font-family:'Plus Jakarta Sans',sans-serif;font-size:16px;font-weight:700;line-height:24px;color:#1f1b10;text-decoration:none;">&#128065; عرض الطلب</a>
                </td>
                </tr>
                </table>
                </td>
                </tr>
                <tr>
                <td style="padding:0 24px 32px;text-align:center;">
                <p style="font-size:11px;line-height:16px;color:#7f765f;margin:0;">Oz School Uniforms</p>
                </td>
                </tr>
                </table>
                </body>
                </html>
                """;
                _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(order.CustomerEmail, $"#{order.OrderNumber} وصل", deliveredHtml));
                break;

            case "cod_failed":
                if (order.State >= OrderState.CodFailed)
                    return Ok(new { status = "ok" });

                if (order.State != OrderState.InTransit)
                    return Ok(new { status = "ok", note = "unexpected_state" });

                order.State = OrderState.CodFailed;
                order.StateChangedAt = now;
                order.CodFailedAt = now;
                await _db.SaveChangesAsync();
                await _auditLog.WriteAsync(Guid.Empty, "order.webhook.cod_failed", "order", order.Id.ToString());

                _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(order.CustomerEmail, $"#{order.OrderNumber} فشل دفع", $"""
                <!DOCTYPE html>
                <html lang="ar" dir="rtl">
                <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width,initial-scale=1">
                <title>فشل الدفع</title>
                <link rel="preconnect" href="https://fonts.googleapis.com">
                <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@700;800&family=Work+Sans:wght@400;600&display=swap" rel="stylesheet">
                </head>
                <body style="margin:0;padding:24px 16px;background-color:#fff8f0;font-family:'Work Sans',sans-serif;direction:rtl;text-align:right;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;width:100%;margin:0 auto;background-color:#ffffff;border:3px solid #1f1b10;border-radius:16px;box-shadow:6px 6px 0 #1f1b10;">
                <tr>
                <td style="padding:32px 24px 0;">
                <p style="font-family:'Plus Jakarta Sans',sans-serif;font-size:12px;font-weight:700;color:#725c00;margin:0 0 8px;">&#9888; فشل الدفع</p>
                <h1 style="font-family:'Plus Jakarta Sans',sans-serif;font-size:32px;font-weight:800;line-height:40px;letter-spacing:-0.01em;color:#1f1b10;margin:0 0 16px;">فشل الدفع!</h1>
                <p style="font-size:16px;line-height:24px;color:#4d4632;margin:0 0 16px;">الدفع بتاع طلبك <strong>#{order.OrderNumber}</strong> (كاش عند الاستلام) فشل.</p>
                <p style="font-size:14px;line-height:20px;color:#7f765f;margin:0 0 24px;">هنتصل بيك قريب عشان نرتب طريقة دفع تانية.</p>
                </td>
                </tr>
                <tr>
                <td style="padding:0 24px 32px;text-align:center;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;">
                <tr>
                <td style="border-radius:8px;border:3px solid #1f1b10;box-shadow:6px 6px 0 #1f1b10;background-color:#ffd200;text-align:center;padding:0;">
                <a href="{trackingUrl}" style="display:block;padding:14px 24px;font-family:'Plus Jakarta Sans',sans-serif;font-size:16px;font-weight:700;line-height:24px;color:#1f1b10;text-decoration:none;">&#128065; عرض الطلب</a>
                </td>
                </tr>
                </table>
                </td>
                </tr>
                <tr>
                <td style="padding:0 24px 32px;text-align:center;">
                <p style="font-size:11px;line-height:16px;color:#7f765f;margin:0;">Oz School Uniforms</p>
                </td>
                </tr>
                </table>
                </body>
                </html>
                """));
                break;

            case "returned_to_store":
                if (order.State >= OrderState.ReturnedToStore)
                    return Ok(new { status = "ok" });

                if (order.State != OrderState.CodFailed)
                    return Ok(new { status = "ok", note = "unexpected_state" });

                order.State = OrderState.ReturnedToStore;
                order.StateChangedAt = now;
                order.ReturnedAt = now;

                foreach (var item in order.Items)
                {
                    if (item.Variant != null)
                    {
                        item.Variant.Stock += item.Qty;
                        item.Variant.UpdatedAt = now;
                    }
                }

                await _db.SaveChangesAsync();
                await _auditLog.WriteAsync(Guid.Empty, "order.webhook.returned_to_store", "order", order.Id.ToString(),
                    reason: "Stock refunded");

                order.State = OrderState.ClosedFailed;
                order.StateChangedAt = now;
                await _db.SaveChangesAsync();
                await _auditLog.WriteAsync(Guid.Empty, "order.webhook.closed_failed", "order", order.Id.ToString());

                _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(order.CustomerEmail, $"#{order.OrderNumber} ملغي", $"""
                <!DOCTYPE html>
                <html lang="ar" dir="rtl">
                <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width,initial-scale=1">
                <title>تم إلغاء الطلب</title>
                <link rel="preconnect" href="https://fonts.googleapis.com">
                <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@700;800&family=Work+Sans:wght@400;600&display=swap" rel="stylesheet">
                </head>
                <body style="margin:0;padding:24px 16px;background-color:#fff8f0;font-family:'Work Sans',sans-serif;direction:rtl;text-align:right;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;width:100%;margin:0 auto;background-color:#ffffff;border:3px solid #1f1b10;border-radius:16px;box-shadow:6px 6px 0 #1f1b10;">
                <tr>
                <td style="padding:32px 24px 0;">
                <p style="font-family:'Plus Jakarta Sans',sans-serif;font-size:12px;font-weight:700;color:#725c00;margin:0 0 8px;">&#10060; طلب ملغي</p>
                <h1 style="font-family:'Plus Jakarta Sans',sans-serif;font-size:32px;font-weight:800;line-height:40px;letter-spacing:-0.01em;color:#1f1b10;margin:0 0 16px;">تم إلغاء الطلب</h1>
                <p style="font-size:16px;line-height:24px;color:#4d4632;margin:0 0 24px;">طلبك <strong>#{order.OrderNumber}</strong> اتلغى. الطلب رجع للمتجر.</p>
                </td>
                </tr>
                <tr>
                <td style="padding:0 24px 32px;text-align:center;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;">
                <tr>
                <td style="border-radius:8px;border:3px solid #1f1b10;box-shadow:6px 6px 0 #1f1b10;background-color:#ffd200;text-align:center;padding:0;">
                <a href="{trackingUrl}" style="display:block;padding:14px 24px;font-family:'Plus Jakarta Sans',sans-serif;font-size:16px;font-weight:700;line-height:24px;color:#1f1b10;text-decoration:none;">&#128065; عرض الطلب</a>
                </td>
                </tr>
                </table>
                </td>
                </tr>
                <tr>
                <td style="padding:0 24px 32px;text-align:center;">
                <p style="font-size:11px;line-height:16px;color:#7f765f;margin:0;">Oz School Uniforms</p>
                </td>
                </tr>
                </table>
                </body>
                </html>
                """));
                break;

            default:
                return BadRequest(new { error = "unknown_status", status });
        }

        return Ok(new { status = "ok" });
    }
}
