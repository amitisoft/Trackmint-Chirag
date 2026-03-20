using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Application.DTOs.Reports;

public sealed class ReportFilterRequest
{
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public Guid? AccountId { get; init; }
    public Guid? CategoryId { get; init; }
    public TransactionType? Type { get; init; }
}

public sealed class CategorySpendReportItem
{
    public required string Category { get; init; }
    public required decimal Amount { get; init; }
    public required string Color { get; init; }
}

public sealed class IncomeExpenseTrendItem
{
    public required string Label { get; init; }
    public required decimal Income { get; init; }
    public required decimal Expense { get; init; }
}

public sealed class AccountBalanceTrendItem
{
    public required string Label { get; init; }
    public required decimal Balance { get; init; }
}

public sealed class ExportFileResponse
{
    public required string FileName { get; init; }
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
}
