using System.Security.Claims;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.DTOs;
using Oz.Api.Jobs;
using Oz.Api.Services;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Tags("Admin - Products")]
[Route("api/v1/admin/products")]
public class ProductAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;
    private readonly IBackgroundJobClient _jobs;

    public ProductAdminController(AppDbContext db, AuditLogService auditLog, IBackgroundJobClient jobs)
    {
        _db = db;
        _auditLog = auditLog;
        _jobs = jobs;
    }

    private Guid GetActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> ListProducts(
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        [FromQuery] long? schoolId = null,
        [FromQuery] long? gradeStageId = null,
        [FromQuery] long? itemTypeId = null,
        [FromQuery] byte? gender = null)
    {
        page = Math.Max(1, page);
        page_size = Math.Clamp(page_size, 1, 100);

        var query = _db.Products.Include(p => p.School).Include(p => p.GradeStage)
            .Include(p => p.ItemType).AsQueryable();

        if (schoolId.HasValue) query = query.Where(p => p.SchoolId == schoolId.Value);
        if (gradeStageId.HasValue) query = query.Where(p => p.GradeStageId == gradeStageId.Value);
        if (itemTypeId.HasValue) query = query.Where(p => p.ItemTypeId == itemTypeId.Value);
        if (gender.HasValue) query = query.Where(p => (byte)p.Gender == gender.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * page_size)
            .Take(page_size)
            .ToListAsync();

        var dtos = items.Select(p => new
        {
            id = p.Id,
            schoolName = p.School?.Name,
            gradeStageName = p.GradeStage?.Name,
            itemType = p.ItemType?.Name,
            gender = (byte)p.Gender,
            color = p.Color,
            isInSet = p.IsInSet,
            isArchived = p.IsArchived,
            createdAt = p.CreatedAt,
            updatedAt = p.UpdatedAt
        });

        return Ok(new { items = dtos, total, page, page_size });
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var actorId = GetActorId();

        var product = new Product
        {
            SchoolId = request.SchoolId,
            GradeStageId = request.GradeStageId,
            ItemTypeId = request.ItemTypeId,
            Gender = (Gender)request.Gender,
            Color = request.Color,
            IsInSet = false,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await _auditLog.WriteAsync(actorId, "product.create", "product", product.Id.ToString(),
            afterJson: JsonSerializer.Serialize(product));

        return Created($"/api/v1/admin/products/{product.Id}", ToDto(product));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateProduct(long id, [FromBody] UpdateProductRequest request)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        var before = JsonSerializer.Serialize(product);

        product.SchoolId = request.SchoolId;
        product.GradeStageId = request.GradeStageId;
        product.ItemTypeId = request.ItemTypeId;
        product.Gender = (Gender)request.Gender;
        product.Color = request.Color;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var after = JsonSerializer.Serialize(product);
        await _auditLog.WriteAsync(GetActorId(), "product.update", "product", id.ToString(), before, after);

        return Ok(ToDto(product));
    }

    [HttpPost("{id:long}/archive")]
    public async Task<IActionResult> ArchiveProduct(long id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        var before = JsonSerializer.Serialize(product);
        product.IsArchived = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditLog.WriteAsync(GetActorId(), "product.archive", "product", id.ToString(),
            before, JsonSerializer.Serialize(product));

        return Ok(ToDto(product));
    }

    [HttpPut("{id:long}/set-flag")]
    public async Task<IActionResult> SetFlag(long id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        var before = JsonSerializer.Serialize(product);
        product.IsInSet = !product.IsInSet;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditLog.WriteAsync(GetActorId(), "product.set_flag", "product", id.ToString(),
            before, JsonSerializer.Serialize(product));

        return Ok(ToDto(product));
    }

    [HttpPost("{productId:long}/variants")]
    public async Task<IActionResult> CreateVariant(long productId, [FromBody] CreateVariantRequest request)
    {
        var productExists = await _db.Products.AnyAsync(p => p.Id == productId);
        if (!productExists) return NotFound();

        var variant = new Variant
        {
            ProductId = productId,
            SizeLabel = request.SizeLabel,
            PriceInclVat = request.PriceInclVat,
            Stock = request.Stock,
            Reserved = request.Reserved,
            LowStockThreshold = request.LowStockThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Variants.Add(variant);
        await _db.SaveChangesAsync();

        await _auditLog.WriteAsync(GetActorId(), "variant.create", "variant", variant.Id.ToString(),
            afterJson: JsonSerializer.Serialize(variant));

        return Created($"/api/v1/admin/variants/{variant.Id}", variant);
    }

    [HttpPut("/api/v1/admin/variants/{id:long}")]
    public async Task<IActionResult> UpdateVariant(long id, [FromBody] UpdateVariantRequest request)
    {
        var variant = await _db.Variants.FindAsync(id);
        if (variant == null) return NotFound();

        var before = JsonSerializer.Serialize(variant);
        variant.SizeLabel = request.SizeLabel;
        variant.PriceInclVat = request.PriceInclVat;
        variant.Stock = request.Stock;
        variant.Reserved = request.Reserved;
        variant.LowStockThreshold = request.LowStockThreshold;
        variant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditLog.WriteAsync(GetActorId(), "variant.update", "variant", id.ToString(),
            before, JsonSerializer.Serialize(variant));

        return Ok(variant);
    }

    [HttpPost("/api/v1/admin/variants/{id:long}/archive")]
    public async Task<IActionResult> ArchiveVariant(long id)
    {
        var variant = await _db.Variants.FindAsync(id);
        if (variant == null) return NotFound();

        var before = JsonSerializer.Serialize(variant);
        variant.IsArchived = true;
        variant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditLog.WriteAsync(GetActorId(), "variant.archive", "variant", id.ToString(),
            before, JsonSerializer.Serialize(variant));

        return Ok(variant);
    }

    [HttpPut("/api/v1/admin/variants/{id:long}/stock")]
    public async Task<IActionResult> UpdateStock(long id, [FromBody] UpdateStockRequest request)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var variant = await _db.Variants
            .FromSqlRaw("SELECT * FROM variant WITH (UPDLOCK, ROWLOCK) WHERE id = {0}", id)
            .FirstOrDefaultAsync();

        if (variant == null) return NotFound();

        var oldStock = variant.Stock;
        var before = JsonSerializer.Serialize(variant);

        variant.Stock = request.Stock;
        if (request.Threshold.HasValue)
            variant.LowStockThreshold = request.Threshold.Value;
        variant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var after = JsonSerializer.Serialize(variant);
        await _auditLog.WriteAsync(GetActorId(), "stock.edit", "variant", id.ToString(), before, after, request.Reason);

        if (oldStock == 0 && request.Stock > 0)
            _jobs.Enqueue<SendNotifyMeEmailsJob>(j => j.ExecuteAsync(id));

        return Ok(variant);
    }

    private static AdminProductDto ToDto(Product p) => new(
        p.Id, p.SchoolId, p.GradeStageId, p.ItemTypeId,
        (byte)p.Gender, p.Color, p.IsInSet, p.IsArchived);
}

public record CreateProductRequest(
    long SchoolId,
    long GradeStageId,
    long ItemTypeId,
    byte Gender,
    string? Color);

public record UpdateProductRequest(
    long SchoolId,
    long GradeStageId,
    long ItemTypeId,
    byte Gender,
    string? Color);

public record CreateVariantRequest(
    string SizeLabel,
    decimal PriceInclVat,
    int Stock = 0,
    int Reserved = 0,
    int LowStockThreshold = 5);

public record UpdateVariantRequest(
    string SizeLabel,
    decimal PriceInclVat,
    int Stock,
    int Reserved,
    int LowStockThreshold);

public record UpdateStockRequest(int Stock, string? Reason = null, int? Threshold = null);
