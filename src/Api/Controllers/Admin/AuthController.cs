using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Email == request.Email);

        if (admin != null)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))).ToLowerInvariant();

            _db.PasswordRecoveries.Add(new PasswordRecovery
            {
                Id = Guid.NewGuid(),
                AdminId = admin.Id,
                CodeHash = codeHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Used = false,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await _auditLog.WriteAsync(admin.Id, "admin.forgot_password", "admin", admin.Id.ToString());

            return Ok(new
            {
                message = "If the email exists, a recovery code has been generated.",
                code
            });
        }

        return Ok(new { message = "If the email exists, a recovery code has been generated." });
    }

    [HttpPost("verify-recovery-code")]
    public async Task<IActionResult> VerifyRecoveryCode([FromBody] VerifyRecoveryCodeRequest request)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Email == request.Email);
        if (admin == null)
            return Unauthorized(new { error = "invalid_code" });

        var recovery = await _db.PasswordRecoveries
            .Where(r => r.AdminId == admin.Id && !r.Used && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (recovery == null)
            return StatusCode(410, new { error = "code_expired" });

        var codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Code))).ToLowerInvariant();

        if (recovery.CodeHash != codeHash)
        {
            recovery.Attempts++;
            if (recovery.Attempts >= 5)
                recovery.Used = true;

            await _db.SaveChangesAsync();
            return Unauthorized(new { error = "invalid_code" });
        }

        recovery.Used = true;
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

        await _auditLog.WriteAsync(admin.Id, "admin.recovery_success", "admin", admin.Id.ToString());

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

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyRecoveryCodeRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
