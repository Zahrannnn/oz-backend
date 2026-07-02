using Hangfire;
using Microsoft.EntityFrameworkCore;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Jobs;

public class AutoCancelOrdersJob
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _jobs;

    public AutoCancelOrdersJob(AppDbContext db, IBackgroundJobClient jobs)
    {
        _db = db;
        _jobs = jobs;
    }

    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        var staleOrders = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Variant)
            .Where(o => (o.State == OrderState.Placed
                         || o.State == OrderState.ReadyToShip
                         || o.State == OrderState.ReadyForPickup)
                        && o.StateChangedAt < now.AddDays(-5))
            .ToListAsync();

        foreach (var order in staleOrders)
        {
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
                Action = "order.auto_cancel",
                EntityType = "order",
                EntityId = order.Id.ToString(),
                CreatedAt = now
            });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            _jobs.Enqueue<SendOrderCancelledEmailJob>(
                j => j.ExecuteAsync(order.Id, order.CustomerEmail, "Auto-cancelled (5 days inactive)", ""));
        }
    }
}
