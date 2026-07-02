using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.DTOs;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Storefront;

[ApiController]
[Tags("Storefront - Schools")]
[Route("api/v1/schools")]
public class StorefrontSchoolsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StorefrontSchoolsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Schools.Where(s => !s.IsArchived);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => EF.Functions.Like(s.Name, $"%{q}%"));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => SchoolListDto.FromEntity(s))
            .ToListAsync(ct);

        return Ok(new { items, total, page, page_size = pageSize });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var school = await _db.Schools
            .Where(s => s.Id == id && !s.IsArchived)
            .Select(s => SchoolListDto.FromEntity(s))
            .FirstOrDefaultAsync(ct);

        if (school is null) return NotFound();
        return Ok(school);
    }
}
