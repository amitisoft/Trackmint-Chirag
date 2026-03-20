using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Domain.Entities;

public sealed class Goal : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateOnly? TargetDate { get; set; }
    public Guid? LinkedAccountId { get; set; }
    public Account? LinkedAccount { get; set; }
    public string Icon { get; set; } = "target";
    public string Color { get; set; } = "#10b981";
    public GoalStatus Status { get; set; } = GoalStatus.Active;
}
