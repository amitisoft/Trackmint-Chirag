using PersonalFinanceTracker.Application.DTOs.Dashboard;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
}
