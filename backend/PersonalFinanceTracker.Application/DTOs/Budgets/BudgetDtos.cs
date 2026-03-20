namespace PersonalFinanceTracker.Application.DTOs.Budgets;

public sealed class BudgetResponse
{
    public required Guid Id { get; init; }
    public required Guid CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required string CategoryColor { get; init; }
    public required int Month { get; init; }
    public required int Year { get; init; }
    public required decimal Amount { get; init; }
    public required decimal ActualSpend { get; init; }
    public required decimal UtilizationPercent { get; init; }
    public required int AlertThresholdPercent { get; init; }
}

public sealed class CreateBudgetRequest
{
    public required Guid CategoryId { get; init; }
    public required int Month { get; init; }
    public required int Year { get; init; }
    public required decimal Amount { get; init; }
    public int AlertThresholdPercent { get; init; } = 80;
}

public sealed class UpdateBudgetRequest
{
    public required decimal Amount { get; init; }
    public required int AlertThresholdPercent { get; init; }
}
