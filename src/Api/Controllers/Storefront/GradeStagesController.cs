using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.DTOs;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Storefront;

[ApiController]
[Tags("Storefront - Schools")]
[Route("api/v1/schools/{schoolId:long}/grade-stages")]
public class StorefrontGradeStagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public StorefrontGradeStagesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> List(long schoolId, CancellationToken ct)
    {
        var schoolExists = await _context.Schools
            .AnyAsync(s => s.Id == schoolId && !s.IsArchived, ct);
        if (!schoolExists) return NotFound();

        var items = await _context.GradeStages
            .Where(g => g.SchoolId == schoolId)
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new GradeStageDto(g.Id, g.SchoolId, g.Name, g.DisplayOrder))
            .ToListAsync(ct);

        return Ok(new { items });
    }
}
