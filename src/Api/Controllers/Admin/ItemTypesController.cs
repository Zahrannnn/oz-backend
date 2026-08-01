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
[Tags("Admin - Item Types")]
[Route("api/v1/admin/item-types")]
public class ItemTypesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;

    public ItemTypesController(AppDbContext db, AuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateItemTypeDto dto, CancellationToken ct)
    {
        var itemType = new ItemType
        {
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow
        };

        _db.ItemTypes.Add(itemType);
        await _db.SaveChangesAsync(ct);

        var actorId = GetActorId();
        await _auditLog.WriteAsync(actorId, "item_type.create", "item_type", itemType.Id.ToString(),
            afterJson: JsonSerializer.Serialize(new { itemType.Id, itemType.Name }));

        var result = new ItemTypeDto(itemType.Id, itemType.Name, itemType.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = itemType.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var total = await _db.ItemTypes.CountAsync(ct);
        var items = await _db.ItemTypes
            .Skip((page - 1) * page_size)
            .Take(page_size)
            .ToListAsync(ct);
        var dtos = items.Select(it => new ItemTypeDto(it.Id, it.Name, it.CreatedAt)).ToList();
        return Ok(new { items = dtos, total, page, page_size });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var itemType = await _db.ItemTypes.FindAsync(new object[] { id }, ct);
        if (itemType is null) return NotFound();
        var dto = new ItemTypeDto(itemType.Id, itemType.Name, itemType.CreatedAt);
        return Ok(dto);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateItemTypeDto dto, CancellationToken ct)
    {
        var itemType = await _db.ItemTypes.FindAsync(new object[] { id }, ct);
        if (itemType is null) return NotFound();

        var beforeJson = JsonSerializer.Serialize(new { itemType.Id, itemType.Name });

        itemType.Name = dto.Name;

        await _db.SaveChangesAsync(ct);

        var afterJson = JsonSerializer.Serialize(new { itemType.Id, itemType.Name });
        var actorId = GetActorId();
        await _auditLog.WriteAsync(actorId, "item_type.update", "item_type", itemType.Id.ToString(), beforeJson, afterJson);

        var result = new ItemTypeDto(itemType.Id, itemType.Name, itemType.CreatedAt);
        return Ok(result);
    }

    [HttpPost("{id:long}/archive")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
    {
        var itemType = await _db.ItemTypes.FindAsync(new object[] { id }, ct);
        if (itemType is null) return NotFound();

        _db.ItemTypes.Remove(itemType);
        await _db.SaveChangesAsync(ct);

        var actorId = GetActorId();
        await _auditLog.WriteAsync(actorId, "item_type.delete", "item_type", id.ToString(),
            reason: "Archived by admin");

        return Ok(new { message = "Item type archived" });
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id)
    {
        return StatusCode(405, new { error = "Use POST archive instead" });
    }

    private Guid GetActorId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }
}

public record CreateItemTypeDto(string Name);
public record UpdateItemTypeDto(string Name);
public record ItemTypeDto(long Id, string Name, DateTime CreatedAt);
