using PersonalFinanceTracker.Application.DTOs.Insights;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IInsightsService
{
    Task<FinancialHealthScoreResponse> GetHealthScoreAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<InsightCardResponse>> GetInsightsAsync(CancellationToken cancellationToken);
}
