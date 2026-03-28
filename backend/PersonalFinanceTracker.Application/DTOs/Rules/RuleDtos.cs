using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Application.DTOs.Rules;

public sealed class RuleResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required RuleField ConditionField { get; init; }
    public required RuleOperator ConditionOperator { get; init; }
    public required string ConditionValue { get; init; }
    public required RuleActionType ActionType { get; init; }
    public required string ActionValue { get; init; }
    public required bool IsActive { get; init; }
    public required int Priority { get; init; }
}

public sealed class CreateRuleRequest
{
    public required string Name { get; init; }
    public required RuleField ConditionField { get; init; }
    public required RuleOperator ConditionOperator { get; init; }
    public required string ConditionValue { get; init; }
    public required RuleActionType ActionType { get; init; }
    public required string ActionValue { get; init; }
    public int Priority { get; init; } = 100;
    public bool IsActive { get; init; } = true;
}

public sealed class UpdateRuleRequest
{
    public required string Name { get; init; }
    public required RuleField ConditionField { get; init; }
    public required RuleOperator ConditionOperator { get; init; }
    public required string ConditionValue { get; init; }
    public required RuleActionType ActionType { get; init; }
    public required string ActionValue { get; init; }
    public required int Priority { get; init; }
    public required bool IsActive { get; init; }
}
