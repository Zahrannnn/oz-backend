using System.Security.Claims;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            _jobs.Enqueue<SendOrderShippedEmailJob>(j => j.ExecuteAsync(order.Id, order.CustomerEmail, trackingId, trackingUrl));

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
            state = StateToString(order.State),
            channel = ChannelToString(order.Channel),
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

    private static string StateToString(OrderState state) => state switch
    {
        OrderState.Placed => "placed",
        OrderState.ReadyToShip => "ready_to_ship",
        OrderState.HandedToCourier => "handed_to_courier",
        OrderState.InTransit => "in_transit",
        OrderState.Delivered => "delivered",
        OrderState.CodFailed => "cod_failed",
        OrderState.ReturnedToStore => "returned_to_store",
        OrderState.ReadyForPickup => "ready_for_pickup",
        OrderState.PickedUp => "picked_up",
        OrderState.ClosedSuccess => "closed_success",
        OrderState.ClosedFailed => "closed_failed",
        OrderState.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    private static string ChannelToString(OrderChannel channel) => channel switch
    {
        OrderChannel.Delivery => "delivery",
        OrderChannel.Pickup => "pickup",
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
    };
}
