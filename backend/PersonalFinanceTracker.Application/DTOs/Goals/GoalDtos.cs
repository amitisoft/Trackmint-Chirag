using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Application.DTOs.Goals;

public sealed class GoalResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required decimal TargetAmount { get; init; }
    public required decimal CurrentAmount { get; init; }
    public required decimal ProgressPercent { get; init; }
    public DateOnly? TargetDate { get; init; }
    public Guid? LinkedAccountId { get; init; }
    public required string Icon { get; init; }
    public required string Color { get; init; }
    public required GoalStatus Status { get; init; }
}

public sealed class CreateGoalRequest
{
    public required string Name { get; init; }
    public required decimal TargetAmount { get; init; }
    public DateOnly? TargetDate { get; init; }
    public Guid? LinkedAccountId { get; init; }
    public string Icon { get; init; } = "target";
    public string Color { get; init; } = "#10b981";
}

public sealed class UpdateGoalRequest
{
    public required string Name { get; init; }
    public required decimal TargetAmount { get; init; }
    public DateOnly? TargetDate { get; init; }
    public Guid? LinkedAccountId { get; init; }
    public required string Icon { get; init; }
    public required string Color { get; init; }
    public required GoalStatus Status { get; init; }
}

public sealed class GoalContributionRequest
{
    public required decimal Amount { get; init; }
    public Guid? SourceAccountId { get; init; }
}
