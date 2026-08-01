using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.Services;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Tags("Admin - Audit Log")]
[Route("api/v1/admin/audit-log")]
public class AuditLogController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditLogController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/v1/admin/audit-log
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? actor = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? entity_type = null,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20)
    {
        page = Math.Max(1, page);
        page_size = Math.Clamp(page_size, 1, 100);

        var query = ApplyFilters(actor, action, from, to, entity_type);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * page_size)
            .Take(page_size)
            .Select(a => new {
                a.Id, a.ActorId, a.Action, a.EntityType, a.EntityId,
                a.BeforeJson, a.AfterJson, a.Reason, a.CreatedAt
            })
            .ToListAsync();

        return Ok(new { items, total, page, page_size });
    }

    // GET /api/v1/admin/audit-log/export
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] Guid? actor = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? entity_type = null)
    {
        var query = ApplyFilters(actor, action, from, to, entity_type);

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new { a.Id, a.ActorId, a.Action, a.EntityType, a.EntityId, a.CreatedAt, a.Reason })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Id,ActorId,Action,EntityType,EntityId,CreatedAt,Reason");
        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Id},{log.ActorId},{EscapeCsv(log.Action)},{EscapeCsv(log.EntityType)},{EscapeCsv(log.EntityId)},{log.CreatedAt:O},{EscapeCsv(log.Reason ?? "")}");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "audit-log-export.csv");
    }

    private IQueryable<AuditLog> ApplyFilters(Guid? actor, string? action, DateTime? from, DateTime? to, string? entityType)
    {
        var query = _db.AuditLogs.AsQueryable();
        if (actor.HasValue) query = query.Where(a => a.ActorId == actor.Value);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action);
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to.Value);
        if (!string.IsNullOrEmpty(entityType)) query = query.Where(a => a.EntityType == entityType);
        return query;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
