using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReadyzController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ReadyzController> _logger;

    public ReadyzController(AppDbContext dbContext, ILogger<ReadyzController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var failures = new List<string>();

        // (a) MSSQL reachable
        try
        {
            await _dbContext.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MSSQL connectivity check failed");
            failures.Add("mssql_unreachable");
        }

        // (b) Hangfire schema initialized - check for Hangfire tables
        if (failures.Count == 0)
        {
            try
            {
                var hangfireExists = await _dbContext.Database
                    .ExecuteSqlRawAsync(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Job'") > 0;
                if (!hangfireExists)
                {
                    failures.Add("hangfire_schema_missing");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Hangfire schema check failed");
                failures.Add("hangfire_check_error");
            }
        }

        // (c) Bosta API key env var set
        var bostaKey = Environment.GetEnvironmentVariable("Bosta__ApiKey");
        if (string.IsNullOrWhiteSpace(bostaKey))
        {
            failures.Add("bosta_api_key_missing");
        }

        if (failures.Count > 0)
        {
            return StatusCode(503, new { status = "unhealthy", failures });
        }

        return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
    }
}
