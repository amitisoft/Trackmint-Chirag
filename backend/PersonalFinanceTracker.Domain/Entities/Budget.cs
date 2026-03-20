using PersonalFinanceTracker.Domain.Common;

namespace PersonalFinanceTracker.Domain.Entities;

public sealed class Budget : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public int AlertThresholdPercent { get; set; } = 80;
}
