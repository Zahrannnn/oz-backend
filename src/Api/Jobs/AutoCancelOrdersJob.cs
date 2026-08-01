using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Jobs;

public class AutoCancelOrdersJob
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<AutoCancelOrdersJob> _logger;

    public AutoCancelOrdersJob(AppDbContext db, IBackgroundJobClient jobs, ILogger<AutoCancelOrdersJob> logger)
    {
        _db = db;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task ExecuteAsync(PerformContext context)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["job_id"] = context.BackgroundJob.Id,
                   ["job_type"] = nameof(AutoCancelOrdersJob)
               }))
        {
            await ExecuteCoreAsync();
        }
    }

    public async Task ExecuteCoreAsync()
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

            _logger.LogInformation("Auto-cancel: {Count} stale orders found", staleOrders.Count);

            await using var tx = await _db.Database.BeginTransactionAsync();

            foreach (var order in staleOrders)
            {
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

                _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(null!, order.CustomerEmail, $"Order #{order.Id} Cancelled", $"""
                <!DOCTYPE html>
                <html>
                <head><meta charset="utf-8"><title>Order Cancelled</title></head>
                <body style="font-family:sans-serif;max-width:600px;margin:auto;padding:20px;">
                    <h1>Order Cancelled</h1>
                    <p>Your order <strong>#{order.Id}</strong> has been cancelled.</p>
                    <p>Reason: Auto-cancelled (5 days inactive)</p>
                    <hr /><p style="color:#666;font-size:12px;">Oz School Uniforms</p>
                </body>
                </html>
                """));
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation("Auto-cancel: {Count} orders cancelled", staleOrders.Count);
    }
}