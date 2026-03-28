using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Domain.Entities;

public sealed class Rule : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public RuleField ConditionField { get; set; }
    public RuleOperator ConditionOperator { get; set; }
    public string ConditionValue { get; set; } = string.Empty;
    public RuleActionType ActionType { get; set; }
    public string ActionValue { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 100;
}
