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

                order.State = OrderState.Delivered;
                order.StateChangedAt = now;
                order.DeliveredAt = now;
                await _db.SaveChangesAsync();
                await _auditLog.WriteAsync(Guid.Empty, "order.webhook.delivered", "order", order.Id.ToString());

                order.State = OrderState.ClosedSuccess;
                order.StateChangedAt = now;
                await _db.SaveChangesAsync();
                await _auditLog.WriteAsync(Guid.Empty, "order.webhook.closed_success", "order", order.Id.ToString());

                _jobs.Enqueue<SendOrderDeliveredEmailJob>(j => j.ExecuteAsync(order.Id, order.CustomerEmail, trackingUrl));
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

                _jobs.Enqueue<SendCodFailedEmailJob>(j => j.ExecuteAsync(order.Id, order.CustomerEmail, trackingUrl));
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

                _jobs.Enqueue<SendOrderCancelledEmailJob>(j => j.ExecuteAsync(order.Id, order.CustomerEmail, "Returned to store", trackingUrl));
                break;

            default:
                return BadRequest(new { error = "unknown_status", status });
        }

        return Ok(new { status = "ok" });
    }
}
