using System.Security.Cryptography;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.DTOs;
using Oz.Api.Helpers;
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
        var channel = request.ResolvedChannel == "delivery" ? OrderChannel.Delivery : OrderChannel.Pickup;
        var (customerName, customerPhone, customerEmail, addressLine) = request.ResolveCustomer();
        var items = request.Items;

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

        var total = items.Sum(i =>
        {
            var v = variantMap[i.VariantId];
            return v.PriceInclVat * i.Qty;
        });

        var orderNumber = $"OZ-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}";

        var order = new Order
        {
            OrderNumber = orderNumber,
            TrackingToken = token,
            TrackingTokenHash = tokenHash,
            State = OrderState.Placed,
            Channel = channel,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            CustomerEmail = customerEmail,
            PickupDuration = channel == OrderChannel.Pickup ? request.PickupDuration : null,
            AddressCity = addressLine,
            AddressLine = channel == OrderChannel.Delivery ? addressLine : null,
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

        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        var trackingUrl = !string.IsNullOrWhiteSpace(frontendUrl)
            ? $"{frontendUrl.TrimEnd('/')}/orders/{token}"
            : $"{Request.Scheme}://{Request.Host}/orders/{token}";

        var confirmationHtml = $"""
        <!DOCTYPE html>
        <html dir="rtl">
        <head><meta charset="utf-8"><title>تم تأكيد الطلب</title></head>
        <body style="font-family:sans-serif;max-width:600px;margin:auto;padding:20px;text-align:right;">
            <h1 style="color:#1f1b10;">تم تأكيد الطلب!</h1>
            <p>طلبك <strong>#{orderNumber}</strong> اتعمل بنجاح.</p>
            <p><a href="{trackingUrl}" style="display:inline-block;background:#00658d;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;">تتبع الطلب</a></p>
            <p style="color:#7f765f;font-size:12px;">احتفظ باللينك ده عشان تتابع حالة طلبك.</p>
            <hr style="border:none;border-top:2px solid #e8dcc8;"><p style="color:#7f765f;font-size:12px;">Oz School Uniforms</p>
        </body>
        </html>
        """;
        _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(null!, order.CustomerEmail, $"#{orderNumber} تم", confirmationHtml));

        return CreatedAtAction(null, null, new PlaceOrderResponse
        {
            OrderId = order.Id,
            OrderNumber = orderNumber,
            Token = token,
            TrackingUrl = trackingUrl,
            Total = total,
            State = "placed",
            PickupDuration = order.PickupDuration,
            PickupDurationLabel = OrderHelpers.PickupDurationLabel(order.PickupDuration, order.CreatedAt)
        });
    }

    [HttpGet("by-token/{token}")]
    public async Task<IActionResult> GetOrderByToken(string token)
    {
        var tokenHash = OrderHelpers.TokenToHash(token);
        if (tokenHash == null)
            return NotFound();

        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ItemType)
            .FirstOrDefaultAsync(o => o.TrackingTokenHash == tokenHash);

        if (order == null)
            return NotFound();

        var auditRows = await _db.AuditLogs
            .Where(a => a.EntityType == "order" && a.EntityId == order.Id.ToString() && a.Action.StartsWith("order."))
            .OrderBy(a => a.CreatedAt)
            .Select(a => new { a.Action, a.CreatedAt })
            .ToListAsync();

        var timeline = auditRows.Select(a => new TimelineEntry
        {
            State = a.Action.StartsWith("order.transition.")
                ? a.Action["order.transition.".Length..]
                : a.Action.Replace("order.", ""),
            At = a.CreatedAt
        }).ToList();

        var stateLabel = OrderHelpers.StateLabel(order.State);

        if (timeline.Count == 0)
        {
            timeline.Add(new TimelineEntry
            {
                State = OrderHelpers.StateToString(order.State),
                At = order.StateChangedAt
            });
        }

        return Ok(new OrderStatusResponse
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            State = OrderHelpers.StateToString(order.State),
            StateLabel = stateLabel,
            Channel = order.Channel == OrderChannel.Delivery ? "delivery" : "pickup",
            Total = order.Total,
            CreatedAt = order.CreatedAt,
            BostaTrackingId = order.BostaTrackingId,
            PickupDuration = order.PickupDuration,
            PickupDurationLabel = OrderHelpers.PickupDurationLabel(order.PickupDuration, order.CreatedAt),

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

    [HttpGet("by-phone/{phone}")]
    public async Task<IActionResult> GetOrdersByPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return UnprocessableEntity(new { error = "phone_required", detail = "رقم التليفون مطلوب" });

        var normalized = phone.Trim();
        if (normalized.Length < 10)
            return UnprocessableEntity(new { error = "phone_invalid", detail = "رقم التليفون غير صحيح" });

        var orders = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ItemType)
            .Where(o => o.CustomerPhone == normalized)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = orders.Select(o =>
        {
            var stateLabel = OrderHelpers.StateLabel(o.State);

            return new
            {
                orderId = o.Id,
                orderNumber = o.OrderNumber,
                token = o.TrackingToken,
                state = OrderHelpers.StateToString(o.State),
                stateLabel,
                channel = o.Channel == OrderChannel.Delivery ? "delivery" : "pickup",
                pickupDuration = o.PickupDuration,
                pickupDurationLabel = OrderHelpers.PickupDurationLabel(o.PickupDuration, o.CreatedAt),
                total = o.Total,
                createdAt = o.CreatedAt,
                bostaTrackingId = o.BostaTrackingId,
                items = o.Items.Select(i => new
                {
                    variantId = i.VariantId,
                    qty = i.Qty,
                    unitPriceSnapshot = i.UnitPriceSnapshot,
                    sizeLabel = i.Variant.SizeLabel,
                    itemType = i.Variant.Product.ItemType.Name,
                    color = i.Variant.Product.Color
                })
            };
        }).ToList();

        return Ok(new { items = result, total = result.Count });
    }

    [HttpPost("by-token/{token}/cancel")]
    public async Task<IActionResult> CancelOrderByToken(string token, [FromBody] CancelOrderRequest request)
    {
        var tokenHash = OrderHelpers.TokenToHash(token);
        if (tokenHash == null)
            return NotFound();

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

        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        var trackingUrl = !string.IsNullOrWhiteSpace(frontendUrl)
            ? $"{frontendUrl.TrimEnd('/')}/orders/{token}"
            : $"{Request.Scheme}://{Request.Host}/orders/{token}";

        var reasonText = string.IsNullOrWhiteSpace(request.Reason) ? "طلب من العميل" : request.Reason;
        _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(null!, order.CustomerEmail, $"#{order.OrderNumber} ملغي", EmailTemplates.Wrap(
            "تم إلغاء الطلب", "&#10060; طلب ملغي",
            $"""
            <p style="font-size:16px;line-height:24px;color:#4d4632;margin:0 0 24px;">طلبك <strong>#{order.OrderNumber}</strong> اتعمل إلغاء.</p>
            <p style="font-size:14px;line-height:20px;color:#7f765f;margin:0 0 24px;">السبب: {reasonText}</p>
            """,
            "&#128065; تتبع الطلب", trackingUrl)));

        return Ok(new { state = "cancelled" });
    }
}
