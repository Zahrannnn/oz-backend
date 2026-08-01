using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Tags("Admin - Reports")]
[Route("api/v1/admin/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string groupBy = "day",
        [FromQuery] long? schoolId = null,
        [FromQuery] string? channel = null)
    {
        var query = _db.Orders
            .Where(o => o.State == OrderState.ClosedSuccess && o.CreatedAt >= from && o.CreatedAt <= to);

        if (!string.IsNullOrEmpty(channel))
        {
            var parsed = channel switch
            {
                "delivery" => OrderChannel.Delivery,
                "pickup" => OrderChannel.Pickup,
                _ => (OrderChannel?)null
            };

            if (parsed == null)
                return Problem("Invalid channel value. Must be 'delivery' or 'pickup'.", statusCode: 400);

            query = query.Where(o => o.Channel == parsed.Value);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(o => o.Items.Any(i => i.Variant.Product.SchoolId == schoolId.Value));
        }

        var list = await query
            .Select(o => new { o.Id, o.Total, o.CreatedAt })
            .ToListAsync();

        string GetPeriodKey(DateTime dt) => groupBy switch
        {
            "week" => $"{dt.Year:0000}-W{CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday):00}",
            "month" => $"{dt.Year:0000}-{dt.Month:00}",
            _ => $"{dt.Year:0000}-{dt.Month:00}-{dt.Day:00}"
        };

        var rows = list
            .GroupBy(o => GetPeriodKey(o.CreatedAt))
            .Select(g => new
            {
                period = g.Key,
                orders_count = g.Count(),
                revenue = g.Sum(o => o.Total)
            })
            .OrderBy(r => r.period)
            .ToList();

        var totals = new
        {
            orders_count = list.Count,
            revenue = list.Sum(o => o.Total)
        };

        return Ok(new { rows, totals });
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport()
    {
        var variants = await _db.Variants
            .Include(v => v.Product)
            .ThenInclude(p => p.ItemType)
            .Where(v => !v.IsArchived)
            .OrderBy(v => v.Stock)
            .Select(v => new
            {
                id = v.Id,
                product_name = v.Product.ItemType.Name,
                size = v.SizeLabel,
                stock = v.Stock,
                threshold = v.LowStockThreshold,
                status = v.Stock == 0 ? "out_of_stock" : v.Stock <= v.LowStockThreshold ? "low_stock" : "ok"
            })
            .ToListAsync();

        var lowStockCount = variants.Count(v => v.status != "ok");

        return Ok(new { variants, low_stock_count = lowStockCount });
    }

    [HttpGet("notify-me")]
    public async Task<IActionResult> GetNotifyMeReport()
    {
        var variants = await _db.PendingAlerts
            .Where(a => !a.Notified)
            .GroupBy(a => a.VariantId)
            .Select(g => new
            {
                id = g.Key,
                request_count = g.Count()
            })
            .OrderByDescending(g => g.request_count)
            .ToListAsync();

        var variantIds = variants.Select(v => v.id).ToList();
        var variantMap = await _db.Variants
            .Where(v => variantIds.Contains(v.Id))
            .Include(v => v.Product)
            .ThenInclude(p => p.ItemType)
            .ToDictionaryAsync(v => v.Id);

        var result = variants.Select(v => new
        {
            id = v.id,
            product_name = variantMap.TryGetValue(v.id, out var vv) ? vv.Product.ItemType.Name : "unknown",
            size = vv?.SizeLabel ?? "unknown",
            request_count = v.request_count
        }).ToList();

        return Ok(new { variants = result });
    }
}
