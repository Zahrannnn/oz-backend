using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oz.Api.Services;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Tags("Admin - Auth")]
[Route("api/v1/admin/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly AuditLogService _auditLog;

    public AuthController(AppDbContext db, JwtService jwt, AuditLogService auditLog)
    {
        _db = db;
        _jwt = jwt;
        _auditLog = auditLog;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Email == request.Email);
        if (admin == null)
        {
            await Task.Delay(Random.Shared.Next(100, 300));
            return Unauthorized(new { error = "invalid_credentials" });
        }

        if (admin.LockedUntil > DateTime.UtcNow)
        {
            return StatusCode(423, new { error = "account_locked", lockedUntil = admin.LockedUntil });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
        {
            admin.FailedAttempts++;
            if (admin.FailedAttempts >= 5)
                admin.LockedUntil = DateTime.UtcNow.AddMinutes(15);
            await _db.SaveChangesAsync();

            await _auditLog.WriteAsync(admin.Id, "admin.login_failed", "admin", admin.Id.ToString());
            return Unauthorized(new { error = "invalid_credentials" });
        }

        admin.FailedAttempts = 0;
        admin.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(admin);

        Response.Cookies.Append("admin_session", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromHours(8)
        });

        await _auditLog.WriteAsync(admin.Id, "admin.login_success", "admin", admin.Id.ToString());

        return Ok(new
        {
            adminId = admin.Id.ToString(),
            email = admin.Email,
            token,
            expiresAt = DateTime.UtcNow.AddHours(8)
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("admin_session", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
        return Ok(new { message = "logged_out" });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var expClaim = User.FindFirst("exp")?.Value;

        return Ok(new
        {
            adminId,
            email,
            expiresAt = expClaim != null
                ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime
                : (DateTime?)null
        });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
