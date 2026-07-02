using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Services;

public class AuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(Guid actorId, string action, string entityType, string entityId,
        string? beforeJson = null, string? afterJson = null, string? reason = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            Reason = reason
        });
        await _db.SaveChangesAsync();
    }
}
