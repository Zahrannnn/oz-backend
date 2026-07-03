using Microsoft.EntityFrameworkCore;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Services;

public class IdempotencyService
{
    private readonly AppDbContext _db;

    public IdempotencyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IdempotencyKey?> GetExistingAsync(string key)
    {
        return await _db.IdempotencyKeys
            .Where(ik => ik.Key == key && ik.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(string key, string? requestHash, int status, string? body)
    {
        _db.IdempotencyKeys.Add(new IdempotencyKey
        {
            Key = key,
            RequestHash = requestHash,
            ResponseStatus = status,
            ResponseBody = body,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
        await _db.SaveChangesAsync();
    }
}
