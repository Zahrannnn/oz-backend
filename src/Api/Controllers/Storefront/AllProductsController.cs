using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.Helpers;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Storefront;

[ApiController]
[Tags("Storefront - Products")]
[Route("api/v1/products")]
public class AllProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AllProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "school")] long? schoolId,
        [FromQuery(Name = "grade_stage")] long? gradeStageId,
        [FromQuery(Name = "item_type")] long? itemTypeId,
        [FromQuery(Name = "gender")] Gender? gender,
        [FromQuery(Name = "search")] string? search,
        [FromQuery(Name = "in_stock")] bool? inStock,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Products.Where(p => !p.IsArchived);

        if (schoolId.HasValue)
            query = query.Where(p => p.SchoolId == schoolId.Value);
        if (gradeStageId.HasValue)
            query = query.Where(p => p.GradeStageId == gradeStageId.Value);
        if (itemTypeId.HasValue)
            query = query.Where(p => p.ItemTypeId == itemTypeId.Value);
        if (gender.HasValue)
            query = query.Where(p => p.Gender == gender.Value);
        if (inStock == true)
            query = query.Where(p => p.Variants.Any(v => !v.IsArchived && v.Stock > 0));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.ItemType.Name.Contains(term)
                || (p.Color != null && p.Color.Contains(term)));
        }

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

        return Ok(new { items, total, page, page_size = pageSize, has_next = hasNext });
    }
}
