namespace PersonalFinanceTracker.Application.DTOs.Dashboard;

public sealed class DashboardSummaryResponse
{
    public required decimal CurrentMonthIncome { get; init; }
    public required decimal CurrentMonthExpense { get; init; }
    public required decimal NetBalance { get; init; }
    public required IReadOnlyCollection<BudgetProgressCard> BudgetProgressCards { get; init; }
    public required IReadOnlyCollection<ChartSlice> SpendingByCategory { get; init; }
    public required IReadOnlyCollection<TrendPoint> IncomeExpenseTrend { get; init; }
    public required IReadOnlyCollection<RecentTransactionItem> RecentTransactions { get; init; }
    public required IReadOnlyCollection<UpcomingRecurringItem> UpcomingRecurringPayments { get; init; }
    public required IReadOnlyCollection<GoalProgressItem> SavingsGoals { get; init; }
}

public sealed class BudgetProgressCard
{
    public required Guid Id { get; init; }
    public required string Category { get; init; }
    public required decimal BudgetAmount { get; init; }
    public required decimal ActualAmount { get; init; }
    public required decimal UtilizationPercent { get; init; }
    public required string Color { get; init; }
}

public sealed class ChartSlice
{
    public required string Label { get; init; }
    public required decimal Value { get; init; }
    public required string Color { get; init; }
}

public sealed class TrendPoint
{
    public required string Label { get; init; }
    public required decimal Income { get; init; }
    public required decimal Expense { get; init; }
}

public sealed class RecentTransactionItem
{
    public required Guid Id { get; init; }
    public required string Merchant { get; init; }
    public required string Category { get; init; }
    public required string Account { get; init; }
    public required string Type { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
}

public sealed class UpcomingRecurringItem
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly NextRunDate { get; init; }
    public required string Frequency { get; init; }
}

public sealed class GoalProgressItem
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required decimal CurrentAmount { get; init; }
    public required decimal TargetAmount { get; init; }
    public required decimal ProgressPercent { get; init; }
    public DateOnly? TargetDate { get; init; }
    public required string Color { get; init; }
}
