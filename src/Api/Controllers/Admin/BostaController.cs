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
            <html>
            <head><meta charset="utf-8"><title>Order Shipped</title></head>
            <body style="font-family:sans-serif;max-width:600px;margin:auto;padding:20px;">
                <h1>Order Shipped!</h1>
                <p>Your order <strong>#{order.Id}</strong> has been shipped.</p>
                <p>Bosta Tracking ID: <strong>{trackingId}</strong></p>
                <p><a href="{trackingUrl}" style="display:inline-block;background:#2563eb;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;">Track Your Order</a></p>
                <hr /><p style="color:#666;font-size:12px;">Oz School Uniforms</p>
            </body>
            </html>
            """;
            _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(order.CustomerEmail, $"Order #{order.Id} Shipped", shippedHtml));

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
