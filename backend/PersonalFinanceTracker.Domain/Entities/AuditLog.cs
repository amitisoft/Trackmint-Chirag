using PersonalFinanceTracker.Domain.Common;

namespace PersonalFinanceTracker.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
