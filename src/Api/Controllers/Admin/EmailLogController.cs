using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Tags("Admin - Email Logs")]
[Route("api/v1/admin/email-logs")]
public class EmailLogController : ControllerBase
{
    private readonly AppDbContext _db;

    public EmailLogController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        page_size = Math.Clamp(page_size, 1, 100);

        var query = _db.EmailLogs.OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * page_size)
            .Take(page_size)
            .Select(e => new
            {
                e.Id,
                e.Recipient,
                e.Template,
                status = e.Status.ToString(),
                e.Error,
                e.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items, total, page, page_size });
    }
}