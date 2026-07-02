using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.DTOs;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Storefront;

[ApiController]
[Tags("Storefront - Products")]
[Route("api/v1/products/{id:long}")]
public class ProductDetailController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductDetailController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(long id, CancellationToken ct = default)
    {
        var product = await _db.Products
            .Include(p => p.School)
            .Include(p => p.GradeStage)
            .Include(p => p.ItemType)
            .Include(p => p.Variants.Where(v => !v.IsArchived))
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsArchived, ct);

        if (product is null)
            return NotFound(new { error = "product_not_found" });

        var dto = new ProductDetailDto(
            product.Id,
            product.School.Name,
            product.GradeStage.Name,
            product.ItemType.Name,
            (byte)product.Gender,
            product.Color,
            product.IsInSet,
            product.Variants
                .OrderBy(v => v.Id)
                .Select(v => new VariantSummaryDto(v.Id, v.SizeLabel, v.PriceInclVat, v.Stock))
                .ToList(),
            product.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageDto(i.Id, i.ProductId, i.Url, i.SortOrder))
                .ToList(),
            product.CreatedAt,
            product.UpdatedAt);

        return Ok(dto);
    }
}
