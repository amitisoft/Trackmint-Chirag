using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Reports;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("category-spend")]
    public Task<IReadOnlyCollection<CategorySpendReportItem>> GetCategorySpend([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken) =>
        reportService.GetCategorySpendAsync(request, cancellationToken);

    [HttpGet("income-vs-expense")]
    public Task<IReadOnlyCollection<IncomeExpenseTrendItem>> GetIncomeExpenseTrend([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken) =>
        reportService.GetIncomeExpenseTrendAsync(request, cancellationToken);

    [HttpGet("account-balance-trend")]
    public Task<IReadOnlyCollection<AccountBalanceTrendItem>> GetAccountBalanceTrend([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken) =>
        reportService.GetAccountBalanceTrendAsync(request, cancellationToken);

    [HttpGet("trends")]
    public Task<ReportTrendsResponse> GetTrends([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken) =>
        reportService.GetTrendsAsync(request, cancellationToken);

    [HttpGet("net-worth")]
    public Task<IReadOnlyCollection<NetWorthPoint>> GetNetWorth([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken) =>
        reportService.GetNetWorthAsync(request, cancellationToken);

    [HttpGet("export/csv")]
    public async Task<FileContentResult> ExportCsv([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
    {
        var file = await reportService.ExportTransactionsCsvAsync(request, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
