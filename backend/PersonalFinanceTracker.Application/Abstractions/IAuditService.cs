namespace PersonalFinanceTracker.Application.Abstractions;

public interface IAuditService
{
    Task WriteAsync(Guid userId, string action, string entityType, Guid? entityId, object metadata, CancellationToken cancellationToken);
}
