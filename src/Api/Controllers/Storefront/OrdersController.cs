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
        var channelStr = request.ResolvedChannel;
        if (channelStr != "delivery" && channelStr != "pickup")
            return UnprocessableEntity(new { error = "channel_invalid", detail = "القناة يجب أن تكون delivery أو pickup" });

        var channel = channelStr == "delivery" ? OrderChannel.Delivery : OrderChannel.Pickup;
        var (customerName, customerPhone, customerEmail, addressLine) = request.ResolveCustomer();

        var items = request.Items;
        if (items.Count == 0)
            return UnprocessableEntity(new { error = "items_required", detail = "مطلوب منتج واحد على الأقل" });

        if (string.IsNullOrWhiteSpace(customerName))
            return UnprocessableEntity(new { error = "name_required", detail = "الاسم مطلوب" });

        if (string.IsNullOrWhiteSpace(customerPhone))
            return UnprocessableEntity(new { error = "phone_required", detail = "رقم التليفون مطلوب" });

        if (string.IsNullOrWhiteSpace(customerEmail))
            return UnprocessableEntity(new { error = "email_required", detail = "البريد الإلكتروني مطلوب" });

        if (channel == OrderChannel.Pickup)
        {
            var dur = request.PickupDuration;
            if (string.IsNullOrWhiteSpace(dur))
                return UnprocessableEntity(new { error = "pickup_duration_req", detail = "مدة الاستلام مطلوبة لطلبات الاستلام" });
            if (dur != "today" && dur != "tomorrow" && dur != "day_after_tomorrow")
                return UnprocessableEntity(new { error = "pickup_duration_inv", detail = "مدة الاستلام غير صحيحة (today / tomorrow / day_after_tomorrow)" });
        }

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
        _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(order.CustomerEmail, $"#{orderNumber} تم", confirmationHtml));

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

        var stateLabel = order.State switch
        {
            OrderState.Placed => "تم الطلب",
            OrderState.ReadyToShip => "جاهز للشحن",
            OrderState.HandedToCourier => "سُلم للمندوب",
            OrderState.InTransit => "في الطريق",
            OrderState.Delivered => "تم التسليم",
            OrderState.CodFailed => "فشل التحصيل",
            OrderState.ReturnedToStore => "مرتجع للمتجر",
            OrderState.ReadyForPickup => "جاهز للاستلام",
            OrderState.PickedUp => "تم الاستلام",
            OrderState.ClosedSuccess => "مكتمل",
            OrderState.ClosedFailed => "مغلق",
            OrderState.Cancelled => "ملغى",
            _ => OrderHelpers.StateToString(order.State)
        };

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
            var stateLabel = o.State switch
            {
                OrderState.Placed => "تم الطلب",
                OrderState.ReadyToShip => "جاهز للشحن",
                OrderState.HandedToCourier => "سُلم للمندوب",
                OrderState.InTransit => "في الطريق",
                OrderState.Delivered => "تم التسليم",
                OrderState.CodFailed => "فشل التحصيل",
                OrderState.ReturnedToStore => "مرتجع للمتجر",
                OrderState.ReadyForPickup => "جاهز للاستلام",
                OrderState.PickedUp => "تم الاستلام",
                OrderState.ClosedSuccess => "مكتمل",
                OrderState.ClosedFailed => "مغلق",
                OrderState.Cancelled => "ملغى",
                _ => OrderHelpers.StateToString(o.State)
            };

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

        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        var trackingUrl = !string.IsNullOrWhiteSpace(frontendUrl)
            ? $"{frontendUrl.TrimEnd('/')}/orders/{token}"
            : $"{Request.Scheme}://{Request.Host}/orders/{token}";

        var reasonText = string.IsNullOrWhiteSpace(request.Reason) ? "طلب من العميل" : request.Reason;
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
        <p style="font-size:16px;line-height:24px;color:#4d4632;margin:0 0 24px;">طلبك <strong>#{order.OrderNumber}</strong> اتعمل إلغاء.</p>
        <p style="font-size:14px;line-height:20px;color:#7f765f;margin:0 0 24px;">السبب: {reasonText}</p>
        </td>
        </tr>
        <tr>
        <td style="padding:0 24px 32px;text-align:center;">
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;">
        <tr>
        <td style="border-radius:8px;border:3px solid #1f1b10;box-shadow:6px 6px 0 #1f1b10;background-color:#ffd200;text-align:center;padding:0;">
        <a href="{trackingUrl}" style="display:block;padding:14px 24px;font-family:'Plus Jakarta Sans',sans-serif;font-size:16px;font-weight:700;line-height:24px;color:#1f1b10;text-decoration:none;">&#128065; تتبع الطلب</a>
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

        return Ok(new { state = "cancelled" });
    }
}
