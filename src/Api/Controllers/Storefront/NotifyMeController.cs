using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;
using Oz.Api.Services;

namespace Oz.Api.Controllers.Storefront;

[ApiController]
[Tags("Storefront - Notify Me")]
[Route("api/v1/variants")]
public class NotifyMeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;

    public NotifyMeController(AppDbContext db, AuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    [HttpPost("{id:long}/notify-me")]
    public async Task<IActionResult> Subscribe(long id, [FromBody] NotifyMeRequest request, CancellationToken ct = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = "email_required" });

        var variant = await _db.Variants.FirstOrDefaultAsync(v => v.Id == id && !v.IsArchived, ct);
        if (variant is null)
            return NotFound(new { error = "variant_not_found" });

        if (variant.Stock > 0)
            return Conflict(new { error = "Variant is in stock" });

        var emailHash = ComputeSha256Hash(email);

        var existing = await _db.PendingAlerts
            .AnyAsync(a => a.VariantId == id && a.EmailHash == emailHash && !a.Notified, ct);

        if (existing)
            return Conflict(new { error = "Already subscribed" });

        var alert = new PendingAlert
        {
            VariantId = id,
            Email = email,
            EmailHash = emailHash,
            Notified = false
        };

        _db.PendingAlerts.Add(alert);
        await _db.SaveChangesAsync(ct);

        await _auditLog.WriteAsync(
            Guid.Empty,
            "notify_me.subscribe",
            "pending_alert",
            alert.Id.ToString(),
            afterJson: JsonSerializer.Serialize(new { variantId = id, emailHash }),
            reason: "Storefront notify-me subscription");

        return Created($"/api/v1/variants/{id}/notify-me", new { id = alert.Id });
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}

public class NotifyMeRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
