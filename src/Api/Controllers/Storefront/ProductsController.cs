using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.Helpers;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Storefront;

[ApiController]
[Tags("Storefront - Products")]
[Route("api/v1/schools/{schoolId:long}/grade-stages/{gradeId:long}/products")]
public class StorefrontProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StorefrontProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        long schoolId,
        long gradeId,
        [FromQuery(Name = "item_type")] long? itemTypeId,
        [FromQuery(Name = "gender")] Gender? gender,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20,
        CancellationToken ct = default)
    {
        var schoolExists = await _db.Schools.AnyAsync(s => s.Id == schoolId, ct);
        if (!schoolExists) return NotFound(new { error = "school_not_found" });

        var gradeExists = await _db.GradeStages.AnyAsync(g => g.Id == gradeId && g.SchoolId == schoolId, ct);
        if (!gradeExists) return NotFound(new { error = "grade_stage_not_found" });

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Products
            .Where(p => p.SchoolId == schoolId
                && p.GradeStageId == gradeId
                && !p.IsArchived);

        if (itemTypeId.HasValue)
            query = query.Where(p => p.ItemTypeId == itemTypeId.Value);
        if (gender.HasValue)
            query = query.Where(p => p.Gender == gender.Value);

        var total = await query.CountAsync(ct);

        var products = await query
            .Include(p => p.ItemType)
            .Include(p => p.Variants.Where(v => !v.IsArchived))
            .Include(p => p.Images)
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = products.Select(ProductCardMapper.Map).ToList();

        var hasNext = (page * pageSize) < total;

        return Ok(new
        {
            items,
            total,
            page,
            page_size = pageSize,
            has_next = hasNext
        });
    }
}
