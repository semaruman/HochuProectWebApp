using System.Text.Json;
using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Infrastructure.Audit;

public interface IAuditService
{
    Task WriteAsync(Guid? actorUserId, string action, string entityType, Guid entityId, object? payload = null, CancellationToken ct = default);
}

public class AuditService(AppDbContext db) : IAuditService
{
    public async Task WriteAsync(Guid? actorUserId, string action, string entityType, Guid entityId, object? payload = null, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PayloadJson = payload is null ? null : JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        });
        await Task.CompletedTask;
    }
}
