using System.Security.Cryptography;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.DTOs;
using Oz.Api.Jobs;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Storefront;

[ApiController]
[Tags("Storefront - Orders")]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _jobs;

    public OrdersController(AppDbContext db, IBackgroundJobClient jobs)
    {
        _db = db;
        _jobs = jobs;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        if (request.Channel != "delivery" && request.Channel != "pickup")
            return UnprocessableEntity(new { detail = "Channel must be 'delivery' or 'pickup'" });

        var channel = request.Channel == "delivery" ? OrderChannel.Delivery : OrderChannel.Pickup;

        var items = request.Items;
        if (items.Count == 0)
            return UnprocessableEntity(new { detail = "At least one item required" });

        await using var tx = await _db.Database.BeginTransactionAsync();

        var sortedIds = items.Select(i => i.VariantId).Distinct().OrderBy(id => id).ToList();
        var variantMap = new Dictionary<long, Variant>();

        foreach (var id in sortedIds)
        {
            var v = await _db.Variants
                .FromSqlRaw("SELECT * FROM variant WITH (UPDLOCK, ROWLOCK) WHERE id = {0}", id)
                .FirstOrDefaultAsync();

            if (v == null)
                return Conflict(new
                {
                    type = "https://tools.ietf.org/html/rfc7807#section-3.1",
                    status = 409,
                    detail = $"Variant {id} not found"
                });

            variantMap[id] = v;
        }

        foreach (var item in items)
        {
            var v = variantMap[item.VariantId];
            if (v.Stock < item.Qty)
                return Conflict(new
                {
                    type = ".../errors/out-of-stock",
                    status = 409,
                    detail = $"Variant {item.VariantId} has {v.Stock} units available; requested {item.Qty}.",
                    errors = new Dictionary<string, string>
                    {
                        [$"items[.](variantId={item.VariantId})"] = "out_of_stock"
                    }
                });
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var tokenHash = SHA256.HashData(tokenBytes);

        var deliveryFee = 0m;
        var total = items.Sum(i =>
        {
            var v = variantMap[i.VariantId];
            return v.PriceInclVat * i.Qty;
        }) + deliveryFee;

        var order = new Order
        {
            TrackingTokenHash = tokenHash,
            State = OrderState.Placed,
            Channel = channel,
            CustomerName = request.Customer.Name,
            CustomerPhone = request.Customer.Phone,
            CustomerEmail = request.Customer.Email,
            AddressCity = request.Customer.AddressCity,
            AddressLine = channel == OrderChannel.Delivery ? request.Customer.AddressLine : null,
            DeliveryFee = deliveryFee,
            Total = total,
            StateChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        foreach (var item in items)
        {
            var v = variantMap[item.VariantId];
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                VariantId = item.VariantId,
                Qty = item.Qty,
                UnitPriceSnapshot = v.PriceInclVat,
                LineTotalSnapshot = v.PriceInclVat * item.Qty,
                CreatedAt = DateTime.UtcNow
            });

            v.Stock -= item.Qty;
            v.UpdatedAt = DateTime.UtcNow;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = Guid.Empty,
            Action = "order.place",
            EntityType = "order",
            EntityId = order.Id.ToString(),
            AfterJson = JsonSerializer.Serialize(new { total, items = items.Select(i => new { i.VariantId, i.Qty }) }),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var domain = $"{Request.Scheme}://{Request.Host}";
        var trackingUrl = $"{domain}/orders/{token}";

        var confirmationHtml = $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>Order Confirmed</title></head>
        <body style="font-family:sans-serif;max-width:600px;margin:auto;padding:20px;">
            <h1>Order Confirmed!</h1>
            <p>Your order <strong>#{order.Id}</strong> has been placed.</p>
            <p><a href="{trackingUrl}" style="display:inline-block;background:#2563eb;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;">Track Your Order</a></p>
            <p><strong>Important:</strong> Save this link to check your order status later.</p>
            <hr /><p style="color:#666;font-size:12px;">Oz School Uniforms</p>
        </body>
        </html>
        """;
        _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(order.CustomerEmail, $"Order #{order.Id} Confirmed", confirmationHtml));

        return CreatedAtAction(null, null, new PlaceOrderResponse
        {
            OrderId = order.Id,
            Token = token,
            TrackingUrl = trackingUrl,
            Total = total,
            State = "placed"
        });
    }

    [HttpGet("by-token/{token}")]
    public async Task<IActionResult> GetOrderByToken(string token)
    {
        var normalizedToken = token.Replace('-', '+').Replace('_', '/');
        var padding = (4 - normalizedToken.Length % 4) % 4;
        normalizedToken += new string('=', padding);

        byte[] tokenBytes;
        try { tokenBytes = Convert.FromBase64String(normalizedToken); }
        catch { return NotFound(); }

        var tokenHash = SHA256.HashData(tokenBytes);

        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ItemType)
            .FirstOrDefaultAsync(o => o.TrackingTokenHash == tokenHash);

        if (order == null)
            return NotFound();

        var timeline = await _db.AuditLogs
            .Where(a => a.EntityType == "order" && a.EntityId == order.Id.ToString() && a.Action.StartsWith("order."))
            .OrderBy(a => a.CreatedAt)
            .Select(a => new TimelineEntry
            {
                State = a.Action.Replace("order.", ""),
                At = a.CreatedAt
            })
            .ToListAsync();

        var stateLabel = order.State switch
        {
            OrderState.Placed => "Placed",
            OrderState.ReadyToShip => "Ready to ship",
            OrderState.HandedToCourier => "Handed to courier",
            OrderState.InTransit => "In transit",
            OrderState.Delivered => "Delivered",
            OrderState.CodFailed => "COD failed",
            OrderState.ReturnedToStore => "Returned to store",
            OrderState.ReadyForPickup => "Ready for pickup",
            OrderState.PickedUp => "Picked up",
            OrderState.ClosedSuccess => "Closed (success)",
            OrderState.ClosedFailed => "Closed (failed)",
            OrderState.Cancelled => "Cancelled",
            _ => order.State.ToString()
        };

        if (timeline.Count == 0)
        {
            timeline.Add(new TimelineEntry
            {
                State = order.State.ToString().ToLower(),
                At = order.StateChangedAt
            });
        }

        return Ok(new OrderStatusResponse
        {
            OrderId = order.Id,
            State = order.State.ToString().ToLower(),
            StateLabel = stateLabel,
            Channel = order.Channel == OrderChannel.Delivery ? "delivery" : "pickup",
            Total = order.Total,
            CreatedAt = order.CreatedAt,
            BostaTrackingId = order.BostaTrackingId,
            Timeline = timeline,
            Items = order.Items.Select(i => new OrderItemStatus
            {
                VariantId = i.VariantId,
                Qty = i.Qty,
                UnitPriceSnapshot = i.UnitPriceSnapshot,
                SizeLabel = i.Variant.SizeLabel,
                ItemType = i.Variant.Product.ItemType.Name,
                Color = i.Variant.Product.Color
            }).ToList()
        });
    }

    [HttpPost("by-token/{token}/cancel")]
    public async Task<IActionResult> CancelOrderByToken(string token, [FromBody] CancelOrderRequest request)
    {
        var normalizedToken = token.Replace('-', '+').Replace('_', '/');
        var padding = (4 - normalizedToken.Length % 4) % 4;
        normalizedToken += new string('=', padding);

        byte[] tokenBytes;
        try { tokenBytes = Convert.FromBase64String(normalizedToken); }
        catch { return NotFound(); }

        var tokenHash = SHA256.HashData(tokenBytes);

        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(o => o.TrackingTokenHash == tokenHash);

        if (order == null)
            return NotFound();

        if (order.State != OrderState.Placed && order.State != OrderState.ReadyToShip && order.State != OrderState.ReadyForPickup)
        {
            return Conflict(new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                status = 409,
                detail = $"Order cannot be cancelled in state {order.State}"
            });
        }

        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync();

        order.State = OrderState.Cancelled;
        order.CancelledAt = now;
        order.StateChangedAt = now;

        foreach (var item in order.Items)
        {
            item.Variant.Stock += item.Qty;
            item.Variant.UpdatedAt = now;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = Guid.Empty,
            Action = "order.cancel",
            EntityType = "order",
            EntityId = order.Id.ToString(),
            AfterJson = JsonSerializer.Serialize(new { state = "cancelled", reason = request.Reason }),
            CreatedAt = now
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var reasonText = string.IsNullOrWhiteSpace(request.Reason) ? "Requested by customer" : request.Reason;
        _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(order.CustomerEmail, $"Order #{order.Id} Cancelled", $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>Order Cancelled</title></head>
        <body style="font-family:sans-serif;max-width:600px;margin:auto;padding:20px;">
            <h1>Order Cancelled</h1>
            <p>Your order <strong>#{order.Id}</strong> has been cancelled.</p>
            <p>Reason: {reasonText}</p>
            <hr /><p style="color:#666;font-size:12px;">Oz School Uniforms</p>
        </body>
        </html>
        """));

        return Ok(new { state = "cancelled" });
    }
}
