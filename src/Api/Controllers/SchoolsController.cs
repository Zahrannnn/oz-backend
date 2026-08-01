using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.DTOs;
using Oz.Api.Services;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers;

[ApiController]
[Authorize]
[Tags("Admin - Schools")]
[Route("api/v1/admin/[controller]")]
public class SchoolsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;

    public SchoolsController(AppDbContext db, AuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSchoolDto dto, CancellationToken ct)
    {
        var school = new School
        {
            Name = dto.Name,
            Type = dto.Type,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Schools.Add(school);
        await _db.SaveChangesAsync(ct);

        var actorId = GetActorId();
        await _auditLog.WriteAsync(actorId, "school.create", "school", school.Id.ToString(),
            afterJson: JsonSerializer.Serialize(new { school.Id, school.Name, school.Type }));

        var result = new SchoolDto(school.Id, school.Name, school.Type, school.IsArchived, school.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = school.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var total = await _db.Schools.CountAsync(ct);
        var items = await _db.Schools
            .Skip((page - 1) * page_size)
            .Take(page_size)
            .ToListAsync(ct);
        var dtos = items.Select(s => new SchoolDto(s.Id, s.Name, s.Type, s.IsArchived, s.CreatedAt)).ToList();
        return Ok(new { items = dtos, total, page, page_size });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var school = await _db.Schools.FindAsync(new object[] { id }, ct);
        if (school is null) return NotFound();
        var dto = new SchoolDto(school.Id, school.Name, school.Type, school.IsArchived, school.CreatedAt);
        return Ok(dto);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSchoolDto dto, CancellationToken ct)
    {
        var school = await _db.Schools.FindAsync(new object[] { id }, ct);
        if (school is null) return NotFound();

        var beforeJson = JsonSerializer.Serialize(new { school.Id, school.Name, school.Type, school.IsArchived });

        school.Name = dto.Name;
        school.Type = dto.Type;
        school.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var afterJson = JsonSerializer.Serialize(new { school.Id, school.Name, school.Type, school.IsArchived });
        var actorId = GetActorId();
        await _auditLog.WriteAsync(actorId, "school.update", "school", school.Id.ToString(), beforeJson, afterJson);

        var result = new SchoolDto(school.Id, school.Name, school.Type, school.IsArchived, school.CreatedAt);
        return Ok(result);
    }

    [HttpPost("{id:long}/archive")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
    {
        var school = await _db.Schools.FindAsync(new object[] { id }, ct);
        if (school is null) return NotFound();

        school.IsArchived = true;
        school.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var actorId = GetActorId();
        await _auditLog.WriteAsync(actorId, "school.archive", "school", school.Id.ToString(),
            reason: "Archived by admin");

        return Ok(new { message = "School archived" });
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

public record UpdateSchoolDto(string Name, SchoolType Type);
