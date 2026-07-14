using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.DTOs;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Storefront;

[ApiController]
[Tags("Storefront - Products")]
[Route("api/v1/products/{productId:long}/images")]
public class ProductImagesStorefrontController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductImagesStorefrontController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(long productId, CancellationToken ct = default)
    {
        var productExists = await _db.Products.AnyAsync(p => p.Id == productId && !p.IsArchived, ct);
        if (!productExists)
            return NotFound(new { error = "product_not_found" });

        var images = await _db.ProductImages
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ProductImageDto(i.Id, i.ProductId, i.Url, i.SortOrder))
            .ToListAsync(ct);

        return Ok(new { items = images });
    }
}
