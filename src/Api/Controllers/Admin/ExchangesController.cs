using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.Services;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Tags("Admin - Orders")]
[Route("api/v1/admin/orders")]
public class ExchangesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;

    public ExchangesController(AppDbContext db, AuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    private Guid GetActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{id:long}/exchanges")]
    public async Task<IActionResult> Create(long id, [FromBody] ExchangeRequest request)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        var orderItem = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);
        if (orderItem == null)
            return NotFound(new { error = "Order item not found" });

        var oldVariant = await _db.Variants.FindAsync(orderItem.VariantId);
        if (oldVariant == null)
            return NotFound(new { error = "Old variant not found" });

        var newVariant = await _db.Variants.FindAsync(request.NewVariantId);
        if (newVariant == null)
            return NotFound(new { error = "New variant not found" });

        if (newVariant.Stock < request.Qty)
            return Conflict(new { error = "Insufficient stock for new variant" });

        var priceDelta = (newVariant.PriceInclVat - orderItem.UnitPriceSnapshot) * request.Qty;

        var before = JsonSerializer.Serialize(new
        {
            order.Id,
            order.Total,
            oldVariantId = oldVariant.Id,
            oldVariantStock = oldVariant.Stock,
            newVariantId = newVariant.Id,
            newVariantStock = newVariant.Stock
        });

        await using var tx = await _db.Database.BeginTransactionAsync();

        oldVariant.Stock += request.Qty;
        newVariant.Stock -= request.Qty;
        order.Total += priceDelta;

        var exchange = new Exchange
        {
            OrderId = order.Id,
            OrderItemId = orderItem.Id,
            OldVariantId = oldVariant.Id,
            NewVariantId = newVariant.Id,
            Qty = request.Qty,
            PriceDelta = priceDelta,
            Reason = request.Reason
        };

        _db.Exchanges.Add(exchange);
        await _db.SaveChangesAsync();

        var after = JsonSerializer.Serialize(new
        {
            order.Id,
            order.Total,
            oldVariantId = oldVariant.Id,
            oldVariantStock = oldVariant.Stock,
            newVariantId = newVariant.Id,
            newVariantStock = newVariant.Stock
        });

        await _auditLog.WriteAsync(GetActorId(), "order.exchange", "order", id.ToString(), before, after, request.Reason);

        await tx.CommitAsync();

        string cashSettlement;
        if (priceDelta > 0)
            cashSettlement = $"parent_pays_{Math.Abs(priceDelta)}";
        else if (priceDelta < 0)
            cashSettlement = $"refund_parent_{Math.Abs(priceDelta)}";
        else
            cashSettlement = "even";

        return Ok(new
        {
            exchangeId = exchange.Id,
            priceDelta,
            newTotal = order.Total,
            cashSettlement
        });
    }
}

public record ExchangeRequest(long OrderItemId, long NewVariantId, int Qty, string? Reason);
