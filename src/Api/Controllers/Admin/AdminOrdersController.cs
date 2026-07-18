using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.Helpers;
using Oz.Api.Services;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Tags("Admin - Orders")]
[Route("api/v1/admin/orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;

    public AdminOrdersController(AppDbContext db, AuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    private Guid GetActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static readonly HashSet<(OrderState from, OrderState to)> DeliveryTransitions = new()
    {
        (OrderState.Placed, OrderState.ReadyToShip),
        (OrderState.Placed, OrderState.Cancelled),
        (OrderState.ReadyToShip, OrderState.HandedToCourier),
        (OrderState.ReadyToShip, OrderState.Cancelled),
        (OrderState.HandedToCourier, OrderState.InTransit),
        (OrderState.InTransit, OrderState.Delivered),
        (OrderState.InTransit, OrderState.CodFailed),
        (OrderState.Delivered, OrderState.ClosedSuccess),
        (OrderState.CodFailed, OrderState.ReturnedToStore),
        (OrderState.ReturnedToStore, OrderState.ClosedFailed),
    };

    private static readonly HashSet<(OrderState from, OrderState to)> PickupTransitions = new()
    {
        (OrderState.Placed, OrderState.ReadyForPickup),
        (OrderState.Placed, OrderState.Cancelled),
        (OrderState.ReadyForPickup, OrderState.PickedUp),
        (OrderState.ReadyForPickup, OrderState.Cancelled),
        (OrderState.PickedUp, OrderState.ClosedSuccess),
    };

    private static HashSet<(OrderState from, OrderState to)> TransitionsFor(OrderChannel channel) =>
        channel == OrderChannel.Delivery ? DeliveryTransitions : PickupTransitions;

    private static List<string> AvailableStates(OrderState current, OrderChannel channel) =>
        TransitionsFor(channel)
            .Where(t => t.from == current)
            .Select(t => OrderHelpers.StateToString(t.to))
            .ToList();

    private static OrderState? TryParseState(string input)
    {
        var pascal = string.Concat(
            input.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant())
        );
        if (Enum.TryParse<OrderState>(pascal, ignoreCase: true, out var result))
            return result;
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] byte? state = null,
        [FromQuery] long? school = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20)
    {
        page = Math.Max(1, page);
        page_size = Math.Clamp(page_size, 1, 100);

        var query = _db.Orders.AsQueryable();

        if (state.HasValue)
        {
            var stateVal = (OrderState)state.Value;
            query = query.Where(o => o.State == stateVal);
        }

        if (school.HasValue)
            query = query.Where(o => o.Items.Any(i => i.Variant.Product.SchoolId == school.Value));

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to.Value);

        if (!string.IsNullOrEmpty(search))
        {
            if (long.TryParse(search, out var searchId))
                query = query.Where(o => o.Id == searchId || EF.Functions.Like(o.CustomerPhone, $"%{search}%"));
            else
                query = query.Where(o => EF.Functions.Like(o.CustomerPhone, $"%{search}%"));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * page_size)
            .Take(page_size)
            .Select(o => new
            {
                id = o.Id,
                orderNumber = o.OrderNumber,
                state = OrderHelpers.StateToString(o.State),
                channel = OrderHelpers.ChannelToString(o.Channel),
                customerName = o.CustomerName,
                customerPhone = o.CustomerPhone,
                total = o.Total,
                createdAt = o.CreatedAt,
                stateChangedAt = o.StateChangedAt
            })
            .ToListAsync();

        return Ok(new { items, total, page, page_size });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ItemType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        var auditLogs = await _db.AuditLogs
            .Where(a => a.EntityType == "order" && a.EntityId == id.ToString())
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                action = a.Action,
                createdAt = a.CreatedAt,
                reason = a.Reason
            })
            .ToListAsync();

        return Ok(ToDetail(order, auditLogs));
    }

    [HttpPost("{id:long}/mark-picked-up")]
    public async Task<IActionResult> MarkPickedUp(long id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ItemType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (order.State != OrderState.ReadyForPickup || order.Channel != OrderChannel.Pickup)
            return Conflict(new { error = "Invalid state or channel" });

        var before = JsonSerializer.Serialize(new { order.Id, state = OrderHelpers.StateToString(order.State), order.StateChangedAt });
        var now = DateTime.UtcNow;

        order.State = OrderState.ClosedSuccess;
        order.PickedUpAt = now;
        order.StateChangedAt = now;

        await _db.SaveChangesAsync();

        var after = JsonSerializer.Serialize(new { order.Id, state = OrderHelpers.StateToString(order.State), order.StateChangedAt });
        await _auditLog.WriteAsync(GetActorId(), "order.picked_up", "order", id.ToString(), before, after);

        return Ok(ToDetail(order, null));
    }

    [HttpPost("{id:long}/transition")]
    public async Task<IActionResult> Transition(long id, [FromBody] OrderTransitionRequest request)
    {
        var target = TryParseState(request.ToState);
        if (target == null)
            return UnprocessableEntity(new { error = $"Invalid state: {request.ToState}" });

        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ItemType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (!TransitionsFor(order.Channel).Contains((order.State, target.Value)))
            return Conflict(new { error = "Invalid transition", from = OrderHelpers.StateToString(order.State), to = request.ToState });

        var before = JsonSerializer.Serialize(new { order.Id, state = OrderHelpers.StateToString(order.State), order.StateChangedAt });
        var now = DateTime.UtcNow;

        order.State = target.Value;
        order.StateChangedAt = now;

        switch (target.Value)
        {
            case OrderState.HandedToCourier:
                order.HandedToCourierAt = now;
                break;
            case OrderState.InTransit:
                order.InTransitAt = now;
                break;
            case OrderState.Delivered:
                order.DeliveredAt = now;
                break;
            case OrderState.CodFailed:
                order.CodFailedAt = now;
                break;
            case OrderState.ReturnedToStore:
                order.ReturnedAt = now;
                break;
            case OrderState.PickedUp:
                order.PickedUpAt = now;
                break;
            case OrderState.Cancelled:
                order.CancelledAt = now;
                break;
        }

        await _db.SaveChangesAsync();

        var after = JsonSerializer.Serialize(new { order.Id, state = OrderHelpers.StateToString(order.State), order.StateChangedAt });
        await _auditLog.WriteAsync(GetActorId(), $"order.transition.{request.ToState}", "order", id.ToString(), before, after);

        return Ok(ToDetail(order, null));
    }

    private object ToDetail(Order order, IEnumerable<dynamic>? timeline)
    {
        var items = order.Items.Select(i => new
        {
            id = i.Id,
            variantId = i.VariantId,
            qty = i.Qty,
            unitPriceSnapshot = i.UnitPriceSnapshot,
            lineTotalSnapshot = i.LineTotalSnapshot,
            sizeLabel = i.Variant?.SizeLabel ?? "",
            itemType = i.Variant?.Product?.ItemType?.Name ?? "",
            color = i.Variant?.Product?.Color
        }).ToList();

        var result = new Dictionary<string, object?>
        {
            ["id"] = order.Id,
            ["orderNumber"] = order.OrderNumber,
            ["state"] = OrderHelpers.StateToString(order.State),
            ["channel"] = OrderHelpers.ChannelToString(order.Channel),
            ["pickupDuration"] = order.PickupDuration,
            ["pickupDurationLabel"] = OrderHelpers.PickupDurationLabel(order.PickupDuration),
            ["customerName"] = order.CustomerName,
            ["customerPhone"] = order.CustomerPhone,
            ["customerEmail"] = order.CustomerEmail,
            ["addressCity"] = order.AddressCity,
            ["addressLine"] = order.AddressLine,
            ["total"] = order.Total,
            ["deliveryFee"] = order.DeliveryFee,
            ["bostaTrackingId"] = order.BostaTrackingId,
            ["createdAt"] = order.CreatedAt,
            ["stateChangedAt"] = order.StateChangedAt,
            ["cancelledAt"] = order.CancelledAt,
            ["deliveredAt"] = order.DeliveredAt,
            ["pickedUpAt"] = order.PickedUpAt,
            ["handedToCourierAt"] = order.HandedToCourierAt,
            ["inTransitAt"] = order.InTransitAt,
            ["returnedAt"] = order.ReturnedAt,
            ["codFailedAt"] = order.CodFailedAt,
            ["items"] = items
        };

        result["availableStates"] = AvailableStates(order.State, order.Channel);

        if (timeline != null)
            result["timeline"] = timeline;

        return result;
    }
}

public record OrderTransitionRequest(string ToState);
