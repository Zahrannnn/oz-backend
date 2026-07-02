using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.Services;
using Oz.Domain.Entities;
using Oz.Domain.Repositories;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Tags("Admin - Grade Stages")]
[Route("api/v1/admin/grade-stages")]
public class GradeStagesController : ControllerBase
{
    private readonly IRepository<GradeStage> _repository;
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;

    public GradeStagesController(IRepository<GradeStage> repository, AppDbContext db, AuditLogService auditLog)
    {
        _repository = repository;
        _db = db;
        _auditLog = auditLog;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGradeStageDto dto, CancellationToken ct)
    {
        var gradeStage = new GradeStage
        {
            SchoolId = dto.SchoolId,
            Name = dto.Name,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(gradeStage, ct);

        var actorId = GetActorId();
        await _auditLog.WriteAsync(actorId, "grade_stage.create", "grade_stage", gradeStage.Id.ToString(),
            afterJson: JsonSerializer.Serialize(new { gradeStage.Id, gradeStage.SchoolId, gradeStage.Name, gradeStage.DisplayOrder }));

        var result = new GradeStageDto(gradeStage.Id, gradeStage.SchoolId, gradeStage.Name, gradeStage.DisplayOrder, gradeStage.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = gradeStage.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] long? schoolId, [FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        IQueryable<GradeStage> query = _db.GradeStages;

        if (schoolId.HasValue)
            query = query.Where(gs => gs.SchoolId == schoolId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(gs => gs.DisplayOrder)
            .Skip((page - 1) * page_size)
            .Take(page_size)
            .ToListAsync(ct);

        var dtos = items.Select(gs => new GradeStageDto(gs.Id, gs.SchoolId, gs.Name, gs.DisplayOrder, gs.CreatedAt)).ToList();
        return Ok(new { items = dtos, total, page, page_size });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var gradeStage = await _repository.GetByIdAsync(id, ct);
        if (gradeStage is null) return NotFound();
        var dto = new GradeStageDto(gradeStage.Id, gradeStage.SchoolId, gradeStage.Name, gradeStage.DisplayOrder, gradeStage.CreatedAt);
        return Ok(dto);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateGradeStageDto dto, CancellationToken ct)
    {
        var gradeStage = await _db.GradeStages.FindAsync(new object[] { id }, ct);
        if (gradeStage is null) return NotFound();

        var beforeJson = JsonSerializer.Serialize(new { gradeStage.Id, gradeStage.SchoolId, gradeStage.Name, gradeStage.DisplayOrder });

        gradeStage.SchoolId = dto.SchoolId;
        gradeStage.Name = dto.Name;
        gradeStage.DisplayOrder = dto.DisplayOrder;

        await _db.SaveChangesAsync(ct);

        var afterJson = JsonSerializer.Serialize(new { gradeStage.Id, gradeStage.SchoolId, gradeStage.Name, gradeStage.DisplayOrder });
        var actorId = GetActorId();
        await _auditLog.WriteAsync(actorId, "grade_stage.update", "grade_stage", gradeStage.Id.ToString(), beforeJson, afterJson);

        var result = new GradeStageDto(gradeStage.Id, gradeStage.SchoolId, gradeStage.Name, gradeStage.DisplayOrder, gradeStage.CreatedAt);
        return Ok(result);
    }

    [HttpPost("{id:long}/archive")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
    {
        var gradeStage = await _db.GradeStages.FindAsync(new object[] { id }, ct);
        if (gradeStage is null) return NotFound();

        _db.GradeStages.Remove(gradeStage);
        await _db.SaveChangesAsync(ct);

        var actorId = GetActorId();
        await _auditLog.WriteAsync(actorId, "grade_stage.delete", "grade_stage", id.ToString(),
            reason: "Archived by admin");

        return Ok(new { message = "Grade stage archived" });
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

public record CreateGradeStageDto(long SchoolId, string Name, int DisplayOrder);
public record UpdateGradeStageDto(long SchoolId, string Name, int DisplayOrder);
public record GradeStageDto(long Id, long SchoolId, string Name, int DisplayOrder, DateTime CreatedAt);
