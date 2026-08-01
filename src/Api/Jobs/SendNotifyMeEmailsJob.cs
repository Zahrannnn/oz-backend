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
                null!,
                alert.Email,
                "المنتج رجع في المخزون!",
                $"""
                <!DOCTYPE html>
                <html lang="ar" dir="rtl">
                <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width,initial-scale=1">
                <title>المنتج رجع في المخزون</title>
                <link rel="preconnect" href="https://fonts.googleapis.com">
                <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@700;800&family=Work+Sans:wght@400;600&display=swap" rel="stylesheet">
                </head>
                <body style="margin:0;padding:24px 16px;background-color:#fff8f0;font-family:'Work Sans',sans-serif;direction:rtl;text-align:right;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;width:100%;margin:0 auto;background-color:#ffffff;border:3px solid #1f1b10;border-radius:16px;box-shadow:6px 6px 0 #1f1b10;">
                <tr>
                <td style="padding:32px 24px 0;">
                <p style="font-family:'Plus Jakarta Sans',sans-serif;font-size:12px;font-weight:700;color:#725c00;margin:0 0 8px;">&#127881; تمت إضافة مخزون جديد</p>
                <h1 style="font-family:'Plus Jakarta Sans',sans-serif;font-size:32px;font-weight:800;line-height:40px;letter-spacing:-0.01em;color:#1f1b10;margin:0 0 16px;">المنتج <span style="color:#725c00;">رجع!</span></h1>
                <p style="font-size:16px;line-height:24px;color:#4d4632;margin:0 0 24px;">أخبار حلوة! القطعة اللي طلبتها رجعت في المخزون. اطلبها بسرعة قبل ما تخلص.</p>
                </td>
                </tr>
                <tr>
                <td style="padding:0 24px;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;border:3px solid #1f1b10;border-radius:8px;background-color:#fff8f0;">
                <tr>
                <td style="padding:20px;">
                <p style="font-size:12px;font-weight:600;line-height:18px;color:#4d4632;margin:0 0 4px;">المدرسة</p>
                <p style="font-family:'Plus Jakarta Sans',sans-serif;font-size:20px;font-weight:700;line-height:28px;color:#1f1b10;margin:0 0 16px;">{schoolName}</p>
                <p style="font-size:12px;font-weight:600;line-height:18px;color:#4d4632;margin:0 0 4px;">القطعة</p>
                <p style="font-family:'Plus Jakarta Sans',sans-serif;font-size:20px;font-weight:700;line-height:28px;color:#1f1b10;margin:0 0 16px;">{itemName}</p>
                <p style="font-size:12px;font-weight:600;line-height:18px;color:#4d4632;margin:0 0 4px;">المقاس</p>
                <p style="font-family:'Plus Jakarta Sans',sans-serif;font-size:24px;font-weight:800;line-height:32px;color:#00658d;margin:0;">{variant.SizeLabel}</p>
                </td>
                </tr>
                </table>
                </td>
                </tr>
                <tr>
                <td style="padding:32px 24px;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;">
                <tr>
                <td style="border-radius:8px;border:3px solid #1f1b10;box-shadow:6px 6px 0 #1f1b10;background-color:#ffd200;text-align:center;padding:0;">
                <a href="https://ozuniform.runasp.net" style="display:block;padding:14px 24px;font-family:'Plus Jakarta Sans',sans-serif;font-size:16px;font-weight:700;line-height:24px;color:#1f1b10;text-decoration:none;">&#128722; تسوق الآن</a>
                </td>
                </tr>
                </table>
                </td>
                </tr>
                <tr>
                <td style="padding:0 24px 32px;text-align:center;">
                <p style="font-size:12px;line-height:18px;color:#7f765f;margin:0;">OZ School Uniforms &middot; <a href="#" style="color:#00658d;text-decoration:underline;">إلغاء الاشتراك</a></p>
                </td>
                </tr>
                </table>
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;width:100%;margin:12px auto 0;">
                <tr>
                <td style="text-align:center;padding:0 16px;">
                <p style="font-size:11px;line-height:16px;color:#7f765f;margin:0;">لقد تلقيت الإيميل ده عشان طلبت مننا ننبّك لما المنتج يرجع في المخزون.</p>
                </td>
                </tr>
                </table>
                </body>
                </html>
                """
            ));
        }
    }
}
