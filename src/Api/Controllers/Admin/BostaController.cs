using System.Security.Claims;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.Helpers;
using Oz.Api.Jobs;
using Oz.Api.Services;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Tags("Admin - Bosta")]
[Route("api/v1/admin/orders")]
public class BostaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBostaClient _bostaClient;
    private readonly AuditLogService _auditLog;
    private readonly IBackgroundJobClient _jobs;

    public BostaController(AppDbContext db, IBostaClient bostaClient, AuditLogService auditLog, IBackgroundJobClient jobs)
    {
        _db = db;
        _bostaClient = bostaClient;
        _auditLog = auditLog;
        _jobs = jobs;
    }

    private Guid GetActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{id:long}/bosta-pickup")]
    public async Task<IActionResult> BookPickup(long id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ItemType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        if (order.State != OrderState.ReadyToShip || order.Channel != OrderChannel.Delivery)
            return Conflict(new { error = "Order must be in ReadyToShip state with Delivery channel" });

        try
        {
            var trackingId = await _bostaClient.CreateShipmentAsync(
                order.Id,
                order.CustomerName,
                order.CustomerPhone,
                order.AddressLine ?? "",
                order.Total);

            var before = JsonSerializer.Serialize(new { order.Id, state = order.State.ToString(), order.BostaTrackingId });
            var now = DateTime.UtcNow;

            order.BostaTrackingId = trackingId;
            order.State = OrderState.HandedToCourier;
            order.StateChangedAt = now;
            order.HandedToCourierAt = now;

            await _db.SaveChangesAsync();

            var after = JsonSerializer.Serialize(new { order.Id, state = order.State.ToString(), order.BostaTrackingId });
            await _auditLog.WriteAsync(GetActorId(), "order.bosta_pickup", "order", id.ToString(), before, after);

            var trackingUrl = $"https://bosta.co/tracking/{trackingId}";
            var shippedHtml = $"""
            <!DOCTYPE html>
            <html lang="ar" dir="rtl">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <title>تم الشحن</title>
            <link rel="preconnect" href="https://fonts.googleapis.com">
            <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@700;800&family=Work+Sans:wght@400;600&display=swap" rel="stylesheet">
            </head>
            <body style="margin:0;padding:24px 16px;background-color:#fff8f0;font-family:'Work Sans',sans-serif;direction:rtl;text-align:right;">
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;width:100%;margin:0 auto;background-color:#ffffff;border:3px solid #1f1b10;border-radius:16px;box-shadow:6px 6px 0 #1f1b10;">
            <tr>
            <td style="padding:32px 24px 0;">
            <p style="font-family:'Plus Jakarta Sans',sans-serif;font-size:12px;font-weight:700;color:#725c00;margin:0 0 8px;">&#128666; تم الشحن</p>
            <h1 style="font-family:'Plus Jakarta Sans',sans-serif;font-size:32px;font-weight:800;line-height:40px;letter-spacing:-0.01em;color:#1f1b10;margin:0 0 16px;">طلبك اتشحن!</h1>
            <p style="font-size:16px;line-height:24px;color:#4d4632;margin:0 0 16px;">طلبك <strong>#{order.OrderNumber}</strong> اتشحن وهيوصل لك قريب.</p>
            <p style="font-size:14px;line-height:20px;color:#7f765f;margin:0 0 24px;">رقم التتبع من بوستة: <strong style="color:#1f1b10;">{trackingId}</strong></p>
            </td>
            </tr>
            <tr>
            <td style="padding:0 24px 32px;text-align:center;">
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;">
            <tr>
            <td style="border-radius:8px;border:3px solid #1f1b10;box-shadow:6px 6px 0 #1f1b10;background-color:#ffd200;text-align:center;padding:0;">
            <a href="{trackingUrl}" style="display:block;padding:14px 24px;font-family:'Plus Jakarta Sans',sans-serif;font-size:16px;font-weight:700;line-height:24px;color:#1f1b10;text-decoration:none;">&#128065; تتبع الشحنة</a>
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
            _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(order.CustomerEmail, $"#{order.OrderNumber} اتعمل شحن", shippedHtml));

            return Ok(ToDetail(order));
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = "bosta_error", detail = ex.Message });
        }
    }

    private object ToDetail(Order order)
    {
        var items = order.Items.Select(i => new
        {
            variantId = i.VariantId,
            qty = i.Qty,
            unitPriceSnapshot = i.UnitPriceSnapshot,
            lineTotalSnapshot = i.LineTotalSnapshot,
            sizeLabel = i.Variant?.SizeLabel ?? "",
            itemType = i.Variant?.Product?.ItemType?.Name ?? "",
            color = i.Variant?.Product?.Color
        }).ToList();

        return new
        {
            id = order.Id,
            orderNumber = order.OrderNumber,
            state = OrderHelpers.StateToString(order.State),
            channel = OrderHelpers.ChannelToString(order.Channel),
            customerName = order.CustomerName,
            customerPhone = order.CustomerPhone,
            customerEmail = order.CustomerEmail,
            addressCity = order.AddressCity,
            addressLine = order.AddressLine,
            total = order.Total,
            deliveryFee = order.DeliveryFee,
            bostaTrackingId = order.BostaTrackingId,
            createdAt = order.CreatedAt,
            stateChangedAt = order.StateChangedAt,
            handedToCourierAt = order.HandedToCourierAt,
            items
        };
    }
}
