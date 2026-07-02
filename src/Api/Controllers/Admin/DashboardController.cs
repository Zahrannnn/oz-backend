using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Tags("Admin - Dashboard")]
[Route("api/v1/admin/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var todayStart = now.Date;

        var revenueThisMonth = await _db.Orders
            .Where(o => o.State == OrderState.ClosedSuccess && o.CreatedAt >= monthStart)
            .SumAsync(o => (decimal?)o.Total) ?? 0m;

        var ordersToday = await _db.Orders
            .CountAsync(o => o.CreatedAt >= todayStart);

        var pendingOrders = await _db.Orders
            .CountAsync(o => o.State == OrderState.Placed
                || o.State == OrderState.ReadyToShip
                || o.State == OrderState.ReadyForPickup);

        var lowStockCount = await _db.Variants
            .CountAsync(v => !v.IsArchived && v.Stock <= v.LowStockThreshold);

        var recentActivity = await _db.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new
            {
                id = a.Id,
                action = a.Action,
                entityType = a.EntityType,
                entityId = a.EntityId,
                createdAt = a.CreatedAt
            })
            .ToListAsync();

        var lowStockVariants = await _db.Variants
            .Include(v => v.Product)
            .ThenInclude(p => p.ItemType)
            .Where(v => !v.IsArchived && v.Stock <= v.LowStockThreshold)
            .OrderBy(v => v.Stock)
            .Take(10)
            .Select(v => new
            {
                id = v.Id,
                sizeLabel = v.SizeLabel,
                stock = v.Stock,
                lowStockThreshold = v.LowStockThreshold,
                productName = v.Product.ItemType.Name
            })
            .ToListAsync();

        return Ok(new
        {
            revenueThisMonth,
            ordersToday,
            pendingOrders,
            lowStockCount,
            recentActivity,
            lowStockVariants
        });
    }
}
