using Hangfire;
using Microsoft.EntityFrameworkCore;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Jobs;

public class SendNotifyMeEmailsJob
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _jobs;

    public SendNotifyMeEmailsJob(AppDbContext db, IBackgroundJobClient jobs)
    {
        _db = db;
        _jobs = jobs;
    }

    public async Task ExecuteAsync(long variantId)
    {
        var pendingAlerts = await _db.PendingAlerts
            .Include(pa => pa.Variant)
                .ThenInclude(v => v.Product)
                    .ThenInclude(p => p.School)
            .Include(pa => pa.Variant)
                .ThenInclude(v => v.Product)
                    .ThenInclude(p => p.ItemType)
            .Where(pa => pa.VariantId == variantId && !pa.Notified)
            .ToListAsync();

        if (pendingAlerts.Count == 0) return;

        var now = DateTime.UtcNow;

        foreach (var alert in pendingAlerts)
        {
            alert.Notified = true;
            alert.NotifiedAt = now;
        }

        await _db.SaveChangesAsync();

        foreach (var alert in pendingAlerts)
        {
            var variant = alert.Variant;
            var product = variant.Product;
            var schoolName = product.School?.Name ?? "Oz School Uniforms";
            var itemName = product.ItemType?.Name ?? "Item";

            _jobs.Enqueue<SendEmailJob>(j => j.ExecuteAsync(
                alert.Email,
                "Back in Stock!",
                $"""
                <!DOCTYPE html>
                <html>
                <head><meta charset="utf-8"><title>Back in Stock</title></head>
                <body style="font-family:sans-serif;max-width:600px;margin:auto;padding:20px;">
                    <h1>Back in Stock!</h1>
                    <p>Great news! The item you requested is back in stock:</p>
                    <p style="font-size:16px;"><strong>{schoolName}</strong> &mdash; {itemName} (Size: {variant.SizeLabel})</p>
                    <p style="color:#666;">Order now before it sells out again!</p>
                    <hr /><p style="color:#666;font-size:12px;">Oz School Uniforms</p>
                </body>
                </html>
                """
            ));
        }
    }
}
