using System.Text.Json;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class AuditService(ApplicationDbContext dbContext) : IAuditService
{
    public async Task WriteAsync(Guid userId, string action, string entityType, Guid? entityId, object metadata, CancellationToken cancellationToken)
    {
        var audit = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            MetadataJson = JsonSerializer.Serialize(metadata)
        };

        await dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
