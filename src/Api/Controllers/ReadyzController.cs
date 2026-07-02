using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        try
        {
            await _dbContext.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MSSQL connectivity check failed");
            failures.Add("mssql_unreachable");
        }

        if (failures.Count == 0)
        {
            try
            {
                using var connection = new SqlConnection(_dbContext.Database.GetConnectionString());
                await connection.OpenAsync();
                using var command = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Job'", connection);
                var count = (int)(await command.ExecuteScalarAsync())!;
                if (count == 0)
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
