using PersonalFinanceTracker.Application.DTOs.Reports;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IReportService
{
    Task<IReadOnlyCollection<CategorySpendReportItem>> GetCategorySpendAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<IncomeExpenseTrendItem>> GetIncomeExpenseTrendAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AccountBalanceTrendItem>> GetAccountBalanceTrendAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ExportFileResponse> ExportTransactionsCsvAsync(ReportFilterRequest request, CancellationToken cancellationToken);
}
