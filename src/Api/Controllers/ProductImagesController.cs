using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.DTOs;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers;

[ApiController]
[Authorize]
[Tags("Admin - Products")]
[Route("api/v1/admin/products/{productId:long}/images")]
public class ProductImagesController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProductImagesController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Upload(
        long productId,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        var productExists = await _context.Products.AnyAsync(p => p.Id == productId, ct);
        if (!productExists)
            return NotFound(new { error = "Product not found" });

        if (file is null || file.Length == 0)
            return UnprocessableEntity(new { error = "File is required" });

        if (file.Length > MaxFileSize)
            return UnprocessableEntity(new { error = "File exceeds 5 MB limit" });

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return UnprocessableEntity(new { error = "Allowed extensions: .jpg, .jpeg, .png, .webp" });

        var fileName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var relDir = Path.Combine("uploads", "products", productId.ToString());
        var relPath = Path.Combine(relDir, fileName);
        var absDir = Path.Combine(_env.ContentRootPath, relDir);
        Directory.CreateDirectory(absDir);
        var absPath = Path.Combine(absDir, fileName);

        await using (var stream = System.IO.File.Create(absPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var nextSort = await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(ct) ?? -1;

        var image = new ProductImage
        {
            ProductId = productId,
            Url = "/" + relPath.Replace('\\', '/'),
            SortOrder = nextSort + 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductImages.Add(image);
        await _context.SaveChangesAsync(ct);

        var dto = new ProductImageDto(image.Id, image.ProductId, image.Url, image.SortOrder);
        return CreatedAtAction(
            nameof(GetById),
            new { productId, imageId = image.Id },
            dto);
    }

    [HttpGet("{imageId:long}")]
    public async Task<IActionResult> GetById(long productId, long imageId, CancellationToken ct)
    {
        var image = await _context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, ct);
        if (image is null)
            return NotFound();

        return Ok(new ProductImageDto(image.Id, image.ProductId, image.Url, image.SortOrder));
    }

    [HttpDelete("{imageId:long}")]
    public async Task<IActionResult> Delete(long productId, long imageId, CancellationToken ct)
    {
        var image = await _context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, ct);
        if (image is null)
            return NotFound();

        var relPath = image.Url.TrimStart('/');
        var absPath = Path.Combine(
            _env.ContentRootPath,
            relPath.Replace('/', Path.DirectorySeparatorChar));

        if (System.IO.File.Exists(absPath))
        {
            try { System.IO.File.Delete(absPath); }
            catch { /* best-effort; row still removed */ }
        }

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}
